# Current game features

Last reviewed: 2026-11-04.

This is an overview of the current Unity prototype based on its scripts and project documentation. A feature being implemented in code does not mean it is configured in every scene or verified in Play Mode.

## Project

- Unity 6000.6.2f1 with the Universal Render Pipeline.
- Third-person action gameplay combining traversal, shooting, enemy encounters, and destructible geometry.
- Gameplay scenes include `Test 1`, `SampleScene`, and the dedicated `Enemy Hit Reactions` scene.
- Shared controller and action systems drive player and enemy behavior.

## Player movement and traversal

- Camera-relative movement using the Unity Input System.
- Rigidbody movement with spring-based ground support and configurable gravity.
- `Dodge` uses the existing action lifecycle. Call `TryDodge()` to move in the input direction, falling back to facing when stationary. It sets horizontal velocity to 15 units/second, preserves collision and vertical movement, and stops horizontal movement on completion or cancellation. Duration comes from `PlayerActionsSO.Dodge`. The former jump input (Space or gamepad south button) triggers Dodge while moving. Cooldown and invulnerability are not provided.
- Ledge detection, grabbing, and hanging.
- Climbing onto ledges, jumping between holds, and ejecting away from a ledge.
- Nearby hold selection and debug highlighting for usable holds.
- Hold right mouse button for slow motion, with configurable time scale and restoration when focus is lost.

Default input bindings include WASD movement, Space (or gamepad south button) for dodging, Shift for parkour, and mouse input for attacks and aiming. Action behavior depends on the player's current state.

## Combat and weapons

- Weapon attachment through a configurable socket and local offset.
- Equipping, unequipping, and cancelling weapon attacks.
- Player 1 equips `IceWeapon`, which throws ParticlePack IceLance particles automatically or while attack input is held. Ice casts are unlimited and use the configured fire rate as their cooldown. The rifle remains available as a separate weapon.
- Optional `PlayerAutoAttack`, enabled on Player 1, automatically aims and fires at the nearest living enemy visible on camera and within weapon range, requiring clear lines of sight from both camera and weapon transform. The weapon checks actual muzzle obstruction when firing.
- Mouse aiming against scene geometry, with a fallback aim plane when no surface is hit.
- IceWeapon raises an OnCast Action consumed by IceProjectileDisplay. Particle contacts return through a callback to IceWeapon, which raises OnAttackConfirmed through CombatHandler to Controller and EntityManager. The collision adapter also raises OnImpact for IceImpactDisplay decoration. Shards face their travel direction with the demo’s random roll. The main-scene IceLance random sizes, 80-unit/s speed and particle children are retained without the casting sphere. IceImpactDisplay attaches smaller, randomly sized and tilted crystals to the mapped visible bone (or contacted scenery), embedded 0.2 units at impact, reusing up to 128 shard objects. Rifle shots retain hitscan collision.
- Configurable weapon damage, fire rate, range, ammunition, reload duration, consciousness damage, and physical hit response.
- Magazine consumption, automatic reload on empty, and a manual reload API. Reserve ammunition is unlimited.
- Muzzle flash, moving tracer particles, and scenery impact sparks and dust.
- Enemies currently use only directional attacks, damaging their living target once when execution begins, after preparation, if it is still inside the attack shape. Hits use the existing interaction flow, including attacker stats, critical hits, target defense, and health/death events. Cone and circular attack implementations remain available but are not selected by enemies.
- Enemy attack preparation indicators with configurable materials, range, and fill.

See [ice weapon](ice-weapon.md), [attack formula](attack-formula.md), [automatic rifle details](automatic-rifle.md), and [weapon stats](weapon-stats.md).

## Health and physical reactions

- Entity health, damage, healing, and death events.
- `EntityManager` processes interactions. `Entity` handles level thresholds, XP reset, and `OnLevelUp` notifications; kill-XP awards are not wired.
- Combat stats including defense and critical-hit properties.
- Consciousness damage, unconscious states, and get-up actions.
- Humanoid hitboxes that forward bullet impulses to a physical ragdoll system.
- Configurable ragdoll strength for the whole body, upper body, and lower body.
- Temporary limb softness, physical impact response, and recovery toward animation.
- Health bar component driven by entity health events.

See [ragdoll details](ragdoll-manager.md).

## Enemy AI and spawning

- Enemies idle, pursue targets, traverse navigation links, attack, become unconscious, recover, and die.
- Idle enemies follow their assigned player with a configurable `idleStoppingDistance` (default 10 units), instead of wandering. EnemyManager assigns the player at spawn and keeps the group's player as the idle follow target when an enemy leaves that group. Combat pursuit restores the NavMeshAgent's original stopping distance. Idle follow targets are separate from combat targets and cleared on despawn.
- NavMesh path following and direct traversal between link endpoints.
- Enemy groups specify a target, enemy type, recruitment engagement distance, separate attack trigger range (default 3 units), group limit, simultaneous attacker limit, and attack interval. Each attacker must be within its group's attack range before beginning an attack.
- Group refresh selects nearby eligible enemies and retains attackers and link traversers until their actions finish.
- Enemy spawning and despawning use the controller spawner and object pool.
- Each `EnemyManager` listens to `EntityManager.OnControllerDied(source, target)` while enabled and despawns only enemies it spawned. It no longer subscribes to individual enemy death callbacks.
- Available enemies and active group members are tracked for bulk cleanup.
- Bulk cleanup returns tracked enemies to the pool and stops registered group coroutines.
- A separate `EnemySetup` component can register groups and spawn an initial batch at configured points.
- An orange `PURSUING` debug label appears above enemies actively pursuing a player target. The label is a screen overlay and remains visible through walls.

The enemy type enum contains Normal, Tank, and Boss. These identifiers alone do not establish separate finished behaviors or encounters for each type. Group recruitment currently searches nearby matching enemies rather than filtering by the mission that spawned them.

## Missions

- `MissionManager` automatically discovers concrete `Mission` subclasses in the same assembly.
- Only the selected mission type is instantiated, on a child GameObject owned by the manager.
- Missions can be selected by type or catalog index at runtime.
- Switching to a different mission is blocked while the current one is running.
- Lifecycle states include Ready, Running, Completed, Failed, and Aborted.
- Every mission records UTC start and end timestamps for each run, ending on completion, failure, or abort and clearing both on reset.
- Starting a mission requires the game state to be Playing.
- Mission results contain outcome, elapsed time, score, rewards, and a success flag.
- Manager methods support starting, completing, failing, aborting, and resetting missions.
- The manager's custom inspector provides a Play Mode type selector and mission controls.

### Current mission types

| Mission | Current behavior |
| --- | --- |
| Exterminate | Creates a normal-enemy group and spawns one enemy. Its current settings spawn 1 enemy, allow 3 group members and 1 simultaneous attacker, and use a 15-unit engagement distance. Aborting calls enemy cleanup. |
| Survival | Tracks time while running, resolves the player reference from `GameManager` if needed, and reacts to player death or a missing player with an unsuccessful result. |

### Mission work still needed

- Exterminate does not yet automatically complete when all mission enemies are defeated.
- Exterminate's completion and failure overrides currently log their outcomes; the enemy cleanup call is in its abort override.
- `MissionManager` currently saves `LastResult` and emits `OnMissionCompleted` for Completed only; Failed and Aborted are not handled there.
- Score and reward fields exist, but these mission implementations do not provide a progression or reward-distribution system.
- The selector and controls are editor tools; a player-facing mission selection menu is not implemented by them.

See [mission documentation](missions.md) for additional context. The current code is authoritative where older documentation differs.

## Destruction and environment interaction

- Grid-based mesh carving with chunk rebuilds and exposed cut surfaces.
- Extracted geometry can become physical debris.
- Configurable debris partitioning, separation velocity, mass, lifetime, and impact breakup.
- Climbing holds are rebuilt for affected chunks after carving.
- Nearby hold loading avoids manifesting every hold at once.
- Shell edge calculation supports newly exposed surfaces and chunk boundary handling.
- Geometry validation tools are available for shell edges and debris.

See [destruction holds](DestructionHolds.md), [debris](Debris.md), and [hold rebuild jobs](Hold-Rebuild-Burst-Comparison.md).

## Navigation and animation tooling

- Building and city NavMesh baking tools with editor controls.
- Automatic links between lower navigation surfaces and elevated building surfaces.
- Incremental baking, forced rebuilds, and configurable geometry filtering.
- Player and enemy animation controllers.
- Player rigging for traversal poses, plus foot placement components and physical hit-reaction tooling.

See [building navigation](building-navmesh.md) and the [foot placement component documentation](../Assets/Scripts/Presentation/Animation/FootPlacement/README.md).

## Supporting systems and prototype foundations

- A persistent `GameManager` tracks MainMenu, Playing, and Paused states and locates the player. It currently enters Playing during startup; this is not a complete menu or pause flow.
- A wave manager generates group specifications and enemy counts, advances timed wave phases, and publishes events. The current completion timer is a fixed three seconds, and enemy spawning is not performed by the wave manager itself.
- A network ownership component enables the local owner's controller. This is groundwork, not a complete multiplayer session, movement replication, or mission synchronization system.
- Audio configuration and playback helpers exist; controller registration methods are placeholders.
- Equipment slot and stat data classes exist; a complete inventory or equipment interface is not established by those classes.
- `EquipmentDrop` assets generate equipment of a configured slot and rarity, selecting distinct stat bonuses up to shared `EquipmentRaritySettings` limits and rolling their designer-defined ranges. See [equipment drops](equipment-drops.md).
- `EquipmentDropTable` assets select an `EquipmentDrop` by relative weight and generate one item, optionally restricted to a requested rarity. `EquipmentManager` exposes specific-drop and random generation methods; its prefab references the starter Boots table.
- `RewardManager` listens to `EntityManager.OnControllerLeveledUp`, generates three equipment options, and raises `OnRewardsGenerated(controller, rewards)`. It is configured on the EquipmentManager prefab; UI and selection remain caller-owned.
- Reusable interpolation utilities support animation and effect transitions.

## Verification scope

This document describes the source available on the review date. No gameplay tests, Unity compilation, performance measurements, or standalone build validation were run to create it. Scene references, imported assets, and component configuration still determine which features are active during play.

Grouped enemy spawns enter Pursuing through the same group-assignment method as recruited enemies. Exterminate recruits within 15 units, beyond the idle stopping distance of 10 units, so idle enemies can approach into recruitment range.
