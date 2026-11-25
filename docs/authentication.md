# Player authentication

GameManager owns a plain C# PlayerAuthentication instance and starts anonymous Unity Authentication during startup. Gameplay continues while sign-in runs and remains playable if it fails. No scene or prefab component assignment is required.

The Authentication package supplies Unity Services Core as a dependency. Let Unity Package Manager resolve the new manifest entry and update its lock file on opening the project. The project must be linked to the intended Unity Gaming Services project under Project Settings > Services.

Before calling authenticated services, await `GameManager.Instance.Authentication.SignInAsync()`, then read `PlayerId`. Call from the Unity main thread. Concurrent calls share the current attempt. Failure propagates to callers and a subsequent call retries. Startup logs failures without logging credentials. No automatic retry loop is installed.

The SDK restores cached credentials on subsequent launches. It manages token refresh on supported platforms; after session expiry, call SignInAsync again before using online services. This implementation has no sign-out or credential-clearing path. Anonymous accounts cannot be recovered on another installation without linking an external identity provider. Account linking remains a next step. See [Cloud Save persistent stats](cloud-save.md) for the authenticated storage provider.

The service has no scene callbacks or subscriptions. A pending SDK request may finish after GameManager destruction, but has no callback that changes scene objects. Destroying a duplicate GameManager returns before player setup, and its Start guard prevents a second startup sign-in.

## Verification in Unity

- Allow package resolution and confirm scripts compile.
- Enter Play Mode online. Await SignInAsync and confirm IsSignedIn and a nonempty PlayerId.
- Restart Play Mode and confirm the same cached PlayerId is restored.
- Call SignInAsync twice before completion and confirm both callers share the same task.
- Start offline: confirm the failure is reported and gameplay continues. Reconnect and call SignInAsync again to verify retry.
- Load another scene or create a duplicate GameManager during a pending attempt; verify no duplicate startup request or destroyed-object exception.

Live authentication and Play Mode verification have not yet been performed.

References: [anonymous sign-in](https://docs.unity.com/en-us/authentication/use-anon-sign-in), [session management](https://docs.unity.com/en-us/authentication/session-management).
