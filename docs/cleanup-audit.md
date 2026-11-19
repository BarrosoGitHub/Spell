# Cleanup audit

## DOTween: migrated and removed from Unity assets

The enemy indicator now uses `GameObject.ToFloat` and `Interpolate.Cancel`. Action and climbing cancellation use the custom `Sequence.Cancel` and `CancelInterpolations`. The interpolator captures the initial float once, supports nonpositive durations, and cancels without invoking completion callbacks. Controller action tracking now allows cancellation to stop pending action callbacks.

DOTween plugin files and settings were moved out of `Assets` into ignored `CleanupArchive/DOTween` after the destructive deletion command was blocked. This is a recoverable backup, not an active Unity dependency. DOTWEEN scripting defines and generated IDE references were removed. No DOTween references remain in Assets, Packages or ProjectSettings.

## Attack input repaired

`PlayerControls.inputactions` contained `AttackPressed`, but its importer had `generateWrapperCode: 0`; the generated C# wrapper was stale. Enabled generation and set the wrapper class to `PlayerControls`. Synchronized the wrapper's embedded JSON and action accessor/callback interface, preserving the authored bindings. Weapon input now reads `AttackPressed.IsPressed()` instead of `ParkourPressed.IsPressed()`, supporting held automatic fire.

For future changes, save the Input Actions editor, select `Assets/Input System/PlayerControls.inputactions` and keep **Generate C# Class** enabled in its Inspector. Apply importer changes. Use the exact generated action name, `inputControls.Player.AttackPressed`.

Validation: C# build succeeds (0 errors, 43 warnings across rebuilt project dependencies). Embedded wrapper JSON exactly matches the input asset. Eight standalone checks against the actual interpolator/sequence sources with minimal Unity stubs passed: linear quarter/midpoint, initial-value capture, cancellation without completion, zero/negative duration, normal completion and sequence cancellation/completion. This does not replace Unity play-mode verification of attack timing, disabled objects or input devices.

## Safe cleanup applied

- Removed unused DOTween imports from `ClimbEject`, `Grab` and `ClimbUp`, and unused collection imports from `ClimbUp`.
- Removed the unused Input System import and write-only private `isPlaying` field from the custom `Sequence`.
- Removed an unused editor-only ShaderGraph namespace import from `SwingJointController`.
- Deleted the empty, unreferenced `EdgeObject` component and its `.meta` file.

## Candidates retained for review

Static text/name and serialized GUID scans found no inbound references to the following roots. These are candidates, not proof that creative work or future functionality should be deleted.

| Candidate | Evidence / consideration |
| --- | --- |
| `Assets/Scripts/Game/player2.prefab` and `player2_Assets` | Two unreferenced prefab roots; their GLBs are referenced only by these prefabs. Contains two GLBs and a PNG, about 56 MB total. |
| `Assets/Scripts/Game/Player_Assets/selected.glb` | No inbound serialized GUID references found; about 29 MB. |
| `SwingJointController`, `AudioManager`, `CameraAimOffset`, `WaveSystemManager` | No external class-name or serialized script GUID references found. They contain actual functionality or unfinished systems; retained. |
| `Assets/_Recovery` | Five recovery scenes, about 0.2 MB. Retained because they may contain unsaved work. |

## Items that should not be bulk-deleted

- `Interpolate`/`InterpolateExtensions` are used through extension methods even though their class names are not mentioned by consumers.
- `ShellEdgeValidation` and `DebrisGeometryValidation` are editor menu tools, so lack of scene references is expected.
- Netcode is used by `ClientPlayerMovement.cs`; package absence from scenes alone is insufficient to remove it.
- Starter Assets contains a playground scene included in build settings; it is not an unused sample folder as a whole.
- `Resources/WeaponShotEffect.mat` is loaded by name at runtime.
- Character references and their variants are intentionally preserved by project instructions.

## Scope and verification

Inspected project C# sources, package manifest, build settings, serialized GUID references and asset metadata. No orphan `.meta` files were found before cleanup. Static scans cannot establish usage through all reflection, binary assets, unsaved scenes or future authoring workflows. No broad asset or package deletion was performed.

Validation: `dotnet build Assembly-CSharp.csproj --verbosity quiet --nologo`. This is C# compilation against Unity's generated project, not a Unity player build or a play-mode test.

## Requested cleanup — 2026-11-19

Removed WeaponStats, RangedWeapon, BulletShotEffects, CameraAimOffset, and the old WaveSystemManager/WavePhase/WaveSpecification scripts from Assets. Removed RagdollHitbox.ApplyBulletImpact and updated generated hitbox initialization. RagdollHitbox still maps physics contacts to visible bones for ice decoration. Removed the bullet-only Resources/WeaponShotEffect material.

Moved Blink, Dzeruza/CliffStonePackL, and Plugins/MotionMatching outside Assets into the ignored CleanupArchive/2026-11-19 folder, together with metadata. The removed packs occupy approximately 812 MiB in that recovery archive. Preserved the referenced 1.9 MiB Run_JumpUpHigh_Run clip and its GUID under Assets/Models/Animations/Traversal. Removed cliff prefab instances from Nature Square Arena and Cliffstone Basin. Their original scene files are backed up in the archive. Removed deleted-pack texture bindings from Cliff Floor and the two Cliffstone Basin materials, leaving those as untextured materials. Removed the editor-only CliffStoneMapBuilder, whose source pack has been removed.

Static verification found no remaining references to archived GUIDs in Unity scene, prefab, material, controller, metadata, and asset files. Both edited scenes have complete local object references. Unity compilation, rendering, and Play Mode verification remain pending.
