# Cloud Save persistent stats

CloudSavePersistentStatStorage implements the existing PersistentStatStorage contract using Unity Cloud Save Player Data (default access class). The authenticated player can read and write their levels. This is client-authoritative prototype storage; validated upgrades through Cloud Code and protected data are not implemented.

## Setup and usage

1. Let Unity resolve `com.unity.services.cloudsave` 3.4.0 and regenerate the package lock file. Confirm scripts compile.
2. Add CloudSavePersistentStatStorage to the object containing PersistentStatManager, and assign it to the manager's Storage field. Assign a PersistentStatProfile as usual. No existing scene or prefab currently references PersistentStatManager, so there is no existing storage assignment to replace automatically.
3. At your player setup boundary, authenticate and load the player's entity:

```csharp
var authentication = GameManager.Instance.Authentication;
await authentication.SignInAsync();
await persistentStatManager.LoadAsync(authentication.PlayerId, playerEntity);
```

Call these APIs on Unity's main thread. The storage provider checks that services are initialized, a player is signed in, and the requested ID matches the signed-in player. It does not sign in or discover GameManager itself.

Updates continue through `await persistentStatManager.SetLevelAsync(stat, level)`. The manager applies an upgrade and emits its notification only after the save succeeds. Caller-owned setup should stop using a target if its scene is torn down while loading; the existing manager does not cancel pending operations.

## Data and failure behavior

The `persistentStats` key holds one JSON string produced by JsonUtility, preserving PersistentStatData's private serialized integer fields. Only a missing key returns new zero levels. Empty data, deserialization errors, authentication failures and service errors propagate to the caller; they do not trigger a local fallback or reset the cloud record. PlayerPrefs records are not migrated automatically.

Writes replace the whole record without write locks: the last completed write wins. Await each upgrade before starting the next; simultaneous sessions or overlapping requests can overwrite progress. There is no background autosave, automatic retry, or offline queue. The provider does not hold subscriptions or modify scene objects after requests finish.

## Verification

Diff/static checks have been performed. Unity compilation and live Cloud Save round trips remain unverified. In Unity, test a fresh player (zero levels), save several different stats, restart and reload the same cached account, and verify all levels survive. Confirm an incorrect player ID is rejected, an offline save leaves the manager's levels unchanged, and malformed existing data reports a failure. Check scene teardown during pending operations before adding automatic scene startup loading.

Reference: [Unity Cloud Save SDK tutorial](https://docs.unity.com/en-us/cloud-save/tutorials/unity-sdk).
