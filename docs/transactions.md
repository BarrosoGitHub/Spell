# Transactions

The shared protocol is generic:

- `TransactionContext`: a string `Type` and a `Dictionary<string, object>` called `Data`.
- `TransactionValidation`: `Validated`, `Code`, and a result `Data` dictionary.
- `TransactionManager`: exposes named methods that construct each operation's context, then call the private `SubmitAsync` transport method. It changes no local state.

The public methods are `SubmitGetStateAsync()`, `SubmitSkillUpgradeAsync(skillId, expectedLevel)`, and `SubmitPersistentStatAsync(key, expectedValue)`. Add another named method and its cloud branch when implementing a new transaction. Payload dictionaries remain inside the manager and are serialized by the Cloud Code SDK.

```csharp
await authentication.SignInAsync();
var transactions = new TransactionManager();

TransactionValidation validation = await transactions.SubmitSkillUpgradeAsync("slow", currentLevel);
if (validation.Validated)
{
    currentLevel = System.Convert.ToInt32(validation.Data["Level"]);
    currencyManager.Essence.Balance = System.Convert.ToInt32(validation.Data["EssenceBalance"]);
}

validation = await transactions.SubmitPersistentStatAsync("maxHealth", currentValue);
if (validation.Validated)
    currentValue = System.Convert.ToInt32(validation.Data["Value"]);
```

Result numbers can deserialize as different .NET numeric types; use `Convert.ToInt32` rather than assuming boxed `int`. Nested results use the SDK's JSON representation. `SubmitPersistentStatAsync` uses the existing protected `UpgradeValue` operation; it does not call the legacy persistent-stat storage provider.

## Implemented cloud operations

- `GetState`, with empty data: returns balances plus `Skills` and `Values` arrays.
- `UpgradeSkill`, with `skillId` and `expectedLevel`: buys one level using server-priced Essence. Result data contains `SkillId`, `Level`, `EssenceBalance`, and `AetherBalance`.
- `UpgradeValue`, with `key` and `expectedValue`: increases a server-configured, non-negative persistent integer by one using Essence. Result data contains `Key`, `Value`, and the two balances. Values start at zero; skill levels start at one.

The cloud decides ownership, costs, limits, and eligibility. This is not an unrestricted API to write arbitrary keys or choose prices. More operations can use entirely different data, provided their cloud branch validates it. Successful changes save together in one protected Cloud Save item using its write lock.

## Setup

Resolve the Cloud Code package in Unity and deploy `CloudCode/ProcessTransaction.js` as `ProcessTransaction`, with String `type` and JSON `data` parameters. This replaces the previous endpoint parameter contract. There is no automatic scene wiring or startup request.

Provision the Protected Player Data object `transactionState` through trusted server/admin tooling:

```json
{
  "essence": 100,
  "aether": 0,
  "skills": [{ "id": "slow", "level": 1 }],
  "values": [{ "id": "maxHealth", "value": 0 }]
}
```

Missing `skills`/`values` collections mean no owned entries in that category. Missing/invalid player data never causes a reset. Extra fields from earlier prototypes are preserved.

Configure these Private Custom Data objects under custom ID `transactionRules`:

```json
// key: skillUpgrades
{ "skills": [{ "id": "slow", "essenceCosts": [10, 25, 50] }] }

// key: valueUpgrades
{ "values": [{ "id": "maxHealth", "essenceCosts": [10, 25, 50] }] }
```

These are example prices and balances. For skills, cost index 0 buys level 1 -> 2. For values, index 0 buys value 0 -> 1. The cost array defines the upgrade limit. Missing rules throw without saving.

## Failures and ownership

Business rejections return `Validated = false` and an operation-specific code such as `InsufficientEssence`, `LevelChanged`, or `ValueChanged`. Configuration, network, and write-conflict failures throw. Read state after an ambiguous failure. Retry an uncertain upgrade with the same expected level/value: if it already committed, the mismatch prevents another charge. This retry rule belongs to these upgrade operations; a different transaction must define its own retry/eligibility rules. Do not lower/reset these values or reuse their IDs without revisiting replay protection.

The manager only returns a task. Callers own loading UI, error handling, checking their scene/account after awaiting, and copying returned state into local DTOs. Shared `Skill` assets and gameplay effects are unchanged. The new protected `values` collection is separate from the existing client-writable `persistentStats` storage; it does not silently migrate or modify that system.

## Verification

`node --test CloudCode/Tests/ProcessTransaction.test.js` covers both operations, server pricing, repeated/concurrent requests, lost responses, rejections, loading, and invalid data/configuration with mocked Cloud Save APIs. A temporary C# harness also checks the named methods' payloads, private submission visibility, returned validation, and authentication using Unity/UGS stubs. Unity compilation, deployment, live SDK/service calls, and gameplay wiring remain unverified.

References: [Cloud Code calls](https://docs.unity.com/en-us/cloud-code/3.1.0/scripts/how-to-guides/run-scripts/unity-runtime), [JSON script parameters](https://docs.unity.com/en-us/cloud-code/3.1.0/scripts/how-to-guides/write-scripts/unity-editor), and [Cloud Save write locks](https://docs.unity.com/en-us/cloud-save/concepts/write-locks).
