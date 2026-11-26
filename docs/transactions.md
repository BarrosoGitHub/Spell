# Transactions

The implementation has three client pieces: `TransactionContext` describes the request, `TransactionManager.SubmitAsync` sends it to Cloud Code, and `TransactionValidation` contains the decision, balances, and target skill level. The manager keeps no state and changes no scene objects. Currency classes remain DTOs; callers copy validated balances into `CurrencyManager`.

`ProcessTransaction.js` handles `GetState` and `UpgradeSkill` directly. An upgrade reads the player's level and server-defined Essence cost, checks ownership/funds/maximum level, and saves the deduction and level increase together using a Cloud Save write lock. To add another transaction, add an enum value and a cloud branch with that operation's rules.

```csharp
await authentication.SignInAsync();
var transactionManager = new TransactionManager();
var validation = await transactionManager.SubmitAsync(new TransactionContext
{
    Type = TransactionType.UpgradeSkill,
    TargetId = "slow",
    ExpectedLevel = currentLevel
});

if (validation.Validated)
{
    currencyManager.Essence.Balance = validation.EssenceBalance;
    currencyManager.Aether.Balance = validation.AetherBalance;
    currentLevel = validation.Level;
}
```

Use `GetState` with the same `TargetId` to load balances and that skill's level first. Level 0 means the player does not own the requested skill. There is no request hierarchy, handler registry, receipt history, revision tracking, or event wiring.

## Setup

- Resolve the Cloud Code package in Unity and deploy `CloudCode/ProcessTransaction.js` as `ProcessTransaction`. Its parameters are String `type`, String `targetId`, and Numeric `expectedLevel`. No automatic scene wiring or startup requests are added.
- Through trusted server/admin tooling, provision the Protected Player Data object `transactionState`:

```json
{ "essence": 100, "aether": 0, "skills": [{ "id": "slow", "level": 1 }] }
```

- Configure the Private Custom Data object `skillUpgrades`, under custom ID `transactionRules`:

```json
{ "skills": [{ "id": "slow", "essenceCosts": [10, 25, 50] }] }
```

Those balances and prices are examples. Cost index 0 buys level 1 -> 2; the array defines the remaining upgrade steps. Prices and player identity come from the server, never the client. Missing or invalid configuration throws without creating/resetting player data. Existing extra fields from earlier prototypes are ignored and preserved.

## Failures and retries

Business rejections return `Validated = false` with `Code` such as `InsufficientEssence`, `MaxLevelReached`, or `LevelChanged`. Network/configuration/write-conflict failures throw. Refresh with `GetState` after a rejection or ambiguous failure.

Retry an uncertain upgrade with the **same expected level**. If it already committed, the level no longer matches, so the retry returns `LevelChanged` and cannot charge again. This reports current state rather than claiming which request caused the change. There is no automatic retry. Do not decrease skill levels or reuse skill IDs without revisiting this retry rule.

The transaction manager only returns a task. Callers own loading UI, error handling, and checking their scene/account is still current after awaiting before updating local state. Shared `Skill` assets and their gameplay behavior are unchanged; consume the returned per-player level in the owning gameplay system. Existing persistent-stat storage remains a separate client-writable prototype path.

## Verification

Run `node --test CloudCode/Tests/ProcessTransaction.test.js`. Tests mock Cloud Save and cover server pricing, atomic updates, repeated/concurrent requests, lost responses, rejections, loading, and invalid input/configuration. A temporary C# harness also checks client argument mapping, returned validation, failures, and authentication using Unity/UGS stubs. Unity compilation, deployment, live service calls, and gameplay wiring have not been verified.

References: [Unity Cloud Code calls](https://docs.unity.com/en-us/cloud-code/3.1.0/scripts/how-to-guides/run-scripts/unity-runtime), [script parameters](https://docs.unity.com/en-us/cloud-code/3.0.0/scripts/how-to-guides/cross-player-data), and [Cloud Save write locks](https://docs.unity.com/en-us/cloud-save/concepts/write-locks).
