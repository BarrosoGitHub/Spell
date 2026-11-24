# Gameplay effects

Feel is the presentation backend. Gameplay scripts do not depend on Feel or EffectsManager.

The scene owns one EffectsManager. It resolves optional scene references once at startup and owns three disposable listeners:

- PlayerEffectsModule: successful shots, dodge execution, and landings after at least 0.2 scaled seconds airborne.
- CombatEffectsModule: player hits, critical hits, damage received, and kills through EntityManager notifications.
- ProgressionEffectsModule: endless waves, every fifth boss wave, level-up, selected rune rewards, and mission completion.

Each EffectsManager binding selects a trigger, enabled checkbox, MMF_Player, and cooldown in unscaled seconds. Multiple bindings can share a trigger. Each child MMF_Player exposes its individual effects for authoring and preview in Feel's inspector. Disable a binding to suppress new playback; disabling the manager unsubscribes all listeners, stops feedbacks, and restores feedback-owned values. No time-scale or hit-stop effects modify simulation timing.

The arena setup tool is Tools > Effects > Set Up Arena Feel. It creates Assets/Prefab/Effects/ArenaEffects.prefab and a scene instance with explicit references. It does not replace an existing manager. Feel demo scenes are excluded from the import. Feedback uses reusable world-space particle systems and small audio pools; no per-hit GameObjects are instantiated by project code. Camera impulses use the existing Cinemachine camera, and bloom pulses target the arena's global volume. Camera effects are limited to damage, critical hits, kills, dodge, and boss waves.

Ice feedback uses three authored layers: lit ParticlePack shard meshes with angular rotation/gravity, translucent noise-drifted cold vapor, and a short crystalline fracture at contact. Dodge and landing add a fading ground frost patch. Critical hits and kills use larger fragments; casts stay compact. Neon rings and automatic bloom flashes are removed. Materials use the arena lighting and a restrained blue-white palette. Non-ice events use subtle tinted vapor.

`Assets/VFX/FrostPatch.png` is a generated transparent frost texture shared by the ground feedback and existing DodgeIce VFX Graph. The dodge trail deposits stationary patches with varied rotation to reduce visible repetition; its bounded pool and event subscriptions remain unchanged. Original FrozenGround texture is preserved for recovery. `Tools > Effects > Upgrade Arena Visual Feedback` rebuilds particle layers in the ArenaEffects prefab, preserving sound/camera entries and scene references; this is an authoring reset, so save manual particle tuning before running it.

The only gameplay additions are typed notifications: MissionManager.OnMissionSelected, EndlessWaves.OnWaveStarted (after the full batch spawns), and RewardManager.OnRewardSelected (after applying the reward). Enemy pooling needs no per-enemy subscriptions.

To add an effect, add a binding and assign an authored MMF_Player. To add a new event source, create a focused listener module owned by EffectsManager, subscribe at setup, and dispose it on disable. Only expose a new event in gameplay when an existing notification cannot describe the occurrence.

## Validation

`Tools > Effects > Run Arena Feedback Smoke Check` enters Play Mode, checks one feedback player per trigger, verifies that every particle feedback layer emits particles, verifies that disabling the manager clears particles, then verifies playback after re-enabling. It returns to Edit Mode. This passed on 2026-11-23. It does not assess subjective sound/visual quality or exercise every gameplay event source.

## Debug damage numbers

DebugManager exposes Show Damage Numbers (enabled by default). DamageNumbersDebugModule listens to EntityManager attack interactions and uses Feel MMFloatingTextMeshPro for rising, fading, camera-facing numbers. Normal damage is white; critical damage is red and slightly larger. The module owns a bounded pool of 64 labels and reuses the oldest when full. Disabling the toggle or manager immediately clears labels and subscriptions. This displays the interaction damage value, not an independently recalculated damage estimate. C# compilation checked; in-game visual validation remains pending.

Normal and critical enemy hits additionally emit a camera-facing crossed ice flash using `Spell/Ice Cross Impact`. It lasts 0.24 seconds, with white cores and cyan edges; critical hits use a larger cross. The existing combat listener supplies its position. `Tools > Effects > Set Up Ice Cross Hit` adds these layers without rebuilding other feedback or restoring disabled dash/audio effects. The linked Cartoon FX reference could not be decoded because the server returned HTML instead of video; this is an original cross-shaped interpretation.
