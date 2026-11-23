# Gameplay effects

Feel is the presentation backend. Gameplay scripts do not depend on Feel or EffectsManager.

The scene owns one EffectsManager. It resolves optional scene references once at startup and owns three disposable listeners:

- PlayerEffectsModule: successful shots, dodge execution, and landings after at least 0.2 scaled seconds airborne.
- CombatEffectsModule: player hits, critical hits, damage received, and kills through EntityManager notifications.
- ProgressionEffectsModule: endless waves, every fifth boss wave, level-up, selected rune rewards, and mission completion.

Each EffectsManager binding selects a trigger, enabled checkbox, MMF_Player, and cooldown in unscaled seconds. Multiple bindings can share a trigger. Each child MMF_Player exposes its individual effects for authoring and preview in Feel's inspector. Disable a binding to suppress new playback; disabling the manager unsubscribes all listeners, stops feedbacks, and restores feedback-owned values. No time-scale or hit-stop effects modify simulation timing.

The arena setup tool is Tools > Effects > Set Up Arena Feel. It creates Assets/Prefab/Effects/ArenaEffects.prefab and a scene instance with explicit references. It does not replace an existing manager. Feel demo scenes are excluded from the import. Feedback uses reusable world-space particle systems and small audio pools; no per-hit GameObjects are instantiated by project code. Camera impulses use the existing Cinemachine camera, and bloom pulses target the arena's global volume. Camera effects are limited to damage, critical hits, kills, dodge, and boss waves.

The only gameplay additions are typed notifications: MissionManager.OnMissionSelected, EndlessWaves.OnWaveStarted (after the full batch spawns), and RewardManager.OnRewardSelected (after applying the reward). Enemy pooling needs no per-enemy subscriptions.

To add an effect, add a binding and assign an authored MMF_Player. To add a new event source, create a focused listener module owned by EffectsManager, subscribe at setup, and dispose it on disable. Only expose a new event in gameplay when an existing notification cannot describe the occurrence.

## Validation

`Tools > Effects > Run Arena Feedback Smoke Check` enters Play Mode, checks one feedback player per trigger, verifies that every trigger emits particles, verifies that disabling the manager clears particles, then verifies playback after re-enabling. It returns to Edit Mode. This passed on 2026-11-23. It does not assess subjective sound/visual quality or exercise every gameplay event source.
