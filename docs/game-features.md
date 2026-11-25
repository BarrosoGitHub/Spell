# Current game features

Last reviewed: 2026-11-04.

This is an overview of the current Unity prototype based on its scripts and project documentation. A feature being implemented in code does not mean it is configured in every scene or verified in Play Mode.

## Project

- Unity 6000.6.2f1 with the Universal Render Pipeline.
- Third-person action gameplay combining traversal, shooting, enemy encounters, and destructible geometry.
- Gameplay scenes include `Test 1`, `SampleScene`, and the dedicated `Enemy Hit Reactions` scene.
- Shared controller and action systems drive player and enemy behavior.

## Player movement and traversal

- Player 1 uses its model’s original bone transforms and mesh bindings, with arm/leg IK and head/chest constraints mapped through its validated Humanoid Avatar.
- Player 1 uses the Hooded Shadowwalker skinned mesh and Humanoid avatar, with its embedded color, metallic and roughness textures configured for URP. The existing Void Sentinel animation takes supply idle, forward and backward locomotion, and the rising airborne state. The existing fall, dodge and climbing motions still drive their respective states through the same player controller.

- Camera-relative movement using the Unity Input System.
- Rigidbody movement with spring-based ground support and configurable gravity.
- `Dodge` uses the existing action lifecycle. Call `TryDodge()` to move in the input direction, falling back to facing when stationary. It sets horizontal velocity to 15 units/second, preserves collision and vertical movement, and stops horizontal movement on completion or cancellation. Duration comes from `PlayerActionsSO.Dodge`. The former jump input (Space or gamepad south button) triggers Dodge while moving. Cooldown and invulnerability are not provided.
- DodgeIceEffect deposits ground-aligned cracked-ice VFX Graph patches every 0.3 units during dodge. A bounded pool of 48 stationary patches leaves a continuous frozen trail that fades after four scaled seconds. Disable clears the trail and destruction releases its objects. Visual validation in Unity remains pending.
- Ledge detection, grabbing, and hanging.
- Climbing onto ledges, jumping between holds, and ejecting away from a ledge.
- Nearby hold selection and debug highlighting for usable holds.
- Hold right mouse button for slow motion, with configurable time scale and restoration when focus is lost.

Default input bindings include WASD movement, Space (or gamepad south button) for dodging, Shift for parkour, and mouse input for attacks and aiming. Action behavior depends on the player's current state.

## Combat and weapons

- Range is an additive stat in horizontal world units, initially 12 on PlayerStats. Profiles, per-level gains, buffs and equipment can provide it. Player auto-attack acquisition, IceWeapon casts, delayed multishots and hit confirmation enforce it. DebugManager’s Show Attack Range toggle draws a cyan ground circle using the current stat, and the stats panel displays it.

- MultishotChance is an additive stat measured in percentage points (25 = 25%). Profiles, level gains, buffs and equipment/rune rolls can supply it. Each full 100 guarantees an extra projectile and the remainder rolls for one more (250 = two guaranteed extras plus a 50% chance of a third). IceWeapon fires extras at 0.1-scaled-second intervals, using the latest aim point supplied by shooting requests before each shot, without another cooldown or recursive roll. Airborne ice projectiles steer toward the weapon’s latest aim point at a configurable 1080 degrees/second, retaining projectile speed and lifetime. Auto-attack refreshes this point while tracking a visible enemy. Existing profiles default to zero. Rune drops can roll multishot bonuses: Common 5–10, Uncommon 10–15, Rare 15–25, Epic 25–35, and Legendary 35–50 percentage points.

- Controller stores the available Skill assets and exposes read access through Skills. Controller enables that list on enable and disables it on disable. Each skill directly subscribes through Enable(controller) and unsubscribes through Disable(controller). Controller uses one serialized list for activation and cleanup, ignoring duplicate entries. Configure the list while disabled so subscriptions remain paired. Skill effects and event subscriptions remain inside each skill. SlowSkill listens to Controller.OnHit after confirmed attack interactions. SlowSkill is assigned to Player 1: 25% chance per hit to apply SlowBuff (-1 movement unit/second for 3 scaled seconds). The existing buff lifecycle prevents stacking the same definition and clears it on death/disable. Skills keep per-use state out of shared assets.

- Weapon attachment through a configurable socket and local offset.
- Equipping, unequipping, and cancelling weapon attacks.
- Player 1 equips `IceWeapon`, which throws ParticlePack IceLance particles automatically or while attack input is held. Ice casts are unlimited and use the configured fire rate as their cooldown.
- Optional `PlayerAutoAttack`, enabled on Player 1, automatically aims and fires at the nearest living enemy visible on camera and within weapon range, requiring clear lines of sight from both camera and weapon transform. The weapon checks actual muzzle obstruction when firing.
- Mouse aiming against scene geometry, with a fallback aim plane when no surface is hit.
- IceWeapon raises an OnCast Action consumed by IceProjectileDisplay. Particle contacts return through a callback to IceWeapon, which raises OnAttackConfirmed through CombatHandler to Controller and EntityManager. The collision adapter also raises OnImpact for IceImpactDisplay decoration. Shards face their travel direction with the demo’s random roll. The main-scene IceLance random sizes, 80-unit/s speed and particle children are retained without the casting sphere. IceImpactDisplay attaches smaller, randomly sized and tilted crystals to the mapped visible bone (or contacted scenery), embedded 0.2 units at impact, reusing up to 128 shard objects.
- Configurable weapon damage, fire rate, range, ammunition, reload duration, physical hit response.
- Magazine consumption, automatic reload on empty, and a manual reload API. Reserve ammunition is unlimited.
- Muzzle flash, moving tracer particles, and scenery impact sparks and dust.
- Mutated enemies use `AttackJump`: a 0.9-second directional warning followed by a 0.75-second committed tackle, up to 7 units with a 1.5-unit arc. During preparation it tracks the target at 90 degrees/second (AttackJump’s PreparationTurnSpeed), then locks its direction at takeoff. The leap stops at NavMesh boundaries and damages its target at most once along the swept footprint. Completion and cancellation restore navigation; EndlessWaves allows mutants to attack from 7 units. The mutant prefab uses the added Jump Attack clip through its own Animator controller. Play Mode tuning remains required.
- Normal enemies use directional attacks, damaging their living target once when execution begins, after preparation, if it is still inside the attack shape. Hits use the existing interaction flow, including attacker stats, critical hits, target defense, and health/death events. Cone and circular attack implementations remain available but are not selected by enemies.
- Enemy attack preparation indicators with configurable materials, range, and fill.

See [ice weapon](ice-weapon.md), [attack formula](attack-formula.md), and [weapon stats](weapon-stats.md).

## Health and physical reactions

- Entity health, damage, healing, and death events.
- `EntityManager` processes interactions. `Entity` handles level thresholds, XP reset, and `OnLevelUp` notifications; kill-XP awards are not wired.
- Combat stats including defense and critical-hit properties.
- Humanoid hitboxes that forward bullet impulses to a physical ragdoll system.
- Configurable ragdoll strength for the whole body, upper body, and lower body.
- Temporary limb softness, physical impact response, and recovery toward animation.
- Health bar component driven by entity health events.

See [ragdoll details](ragdoll-manager.md).

## Enemy AI and spawning

- EnemyAnimationController uses Animation Data → Height Offset as its vertical model offset in world units (editable during Play Mode). Normal and mutated enemies retain a 1-unit offset. Runeheart Colossus uses its own validated Humanoid Avatar, boss rest pose and mesh bone bindings for retargeting the shared enemy clips. Its BossAnimator override uses the looping Humanoid Boss Walking clip for locomotion at 1.6x playback. EnemyAnimationController exposes Locomotion Animation Speed and Face Target; the boss faces the player during pursuit and its primary attack. Runeheart Colossus has a 1.75-unit offset and is scaled to 1.75 times its original size and uses the Boss enemy type and the normal enemy skeleton/animations. EndlessWaves adds one boss on waves 5, 10, 15, etc., alongside the normal and mutated enemies, at the wave’s level. BossStats starts at 50 health and gains 10 per level. Its primary attack marks a 1.5-unit ground radius for 1.2 seconds, then raises the supplied Meshy spike, applying one normal stat-based hit if the target remains inside. Spike visuals reset on cancellation, death and pool reuse.

- Normal enemies blend in a head aim constraint while pursuing a target. The constraint tracks the target at a configurable height and blends out when pursuit ends. With animation LOD enabled, the rig is evaluated at the same cadence as the Animator.

- Enemy_normal uses the Crimson Wraith skinned mesh and its extracted base-color, metallic, and roughness textures with a URP Lit material (roughness is inverted into smoothness and packed into the metallic map alpha). Its Humanoid avatar retargets the existing EnemyAnimatorController clips; the animation scheduler and hit ragdoll remain attached to the model.

- Enemies idle, pursue targets, traverse navigation links, attack and die.
- Idle enemies follow their assigned player with a configurable `idleStoppingDistance` (default 3 units), instead of wandering. EnemyManager assigns the player at spawn and keeps the group's player as the idle follow target when an enemy leaves that group. Combat pursuit restores the NavMeshAgent's original stopping distance. Idle follow targets are separate from combat targets and cleared on despawn. On entering idle, enemies randomly scale their authored NavMesh avoidance radius by 1.3 to 3 and restore it when leaving idle.
- NavMesh path following and direct traversal between link endpoints.
- Enemy groups specify a target, enemy type, recruitment engagement distance, separate attack trigger range (default 3 units), group limit, simultaneous attacker limit, and attack interval. Each attacker must be within its group's attack range before beginning an attack.
- Group refresh selects nearby eligible enemies and retains attackers and link traversers until their actions finish.
- Enemy spawning and despawning use the controller spawner and object pool.
- Add `OptimizationManager` to a scene object and assign its spawner and distance origin. Its Animation Lod boolean creates or removes a child containing `DistanceManager` and `AnimationLodManager`. Disabling the owner removes that child and restores the models.
- `AnimationLodManager` owns distance refresh and animation scheduling. Its Inspector settings default to 30 Hz within 15 units, 15 Hz within 40 units, and 5 Hz beyond. Entries reserve capacity for 1,000 enemies, stagger initial deadlines, and pass full accumulated scaled time to each animation sample. Spawn and despawn events maintain entries, including enemies already spawned when the system starts.
- `AnimatorFrequency` has no per-frame callback. Its `Tick` samples the Animator. The scheduler owns evaluation while active. Manual animation disables the model’s hit ragdoll component, then restores its previous enabled state and Animator settings when released. Play Mode validation of animation scheduling and ragdoll integration remains required.
- Each `EnemyManager` listens to `EntityManager.OnControllerDied(source, target)` while enabled and despawns only enemies it spawned. It no longer subscribes to individual enemy death callbacks.
- Available enemies and active group members are tracked for bulk cleanup.
- Bulk cleanup returns tracked enemies to the pool and stops registered group coroutines.
- `EnemySetup` registers one pursuit group per enemy type. Enemies pursue their assigned target directly, and each enemy can belong to one group registered by its spawning manager.
- Scene-owned `DebugManager` coordinates modular debug features with live Inspector booleans for pursuit labels, enemy level labels, player stats, hold highlights, traversal rays, balance-beam geometry, combat logging, and mission-state logging. Enemy labels are projected above the head and remain visible through walls. Pursuit labels, enemy levels, and player stats start enabled. Other debug features start disabled. Disabling the manager clears highlights and subscriptions. Enemy/camera discovery runs once per unscaled second, and the stats panel refreshes every 0.25 unscaled seconds.

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
| Exterminate | Spawns 100 normal enemies for profiling, using a grid projected onto the NavMesh near world origin. Grid points that miss the NavMesh use the sampled center. The combat group recruits up to 3 enemies within 15 units and allows 1 simultaneous attacker. Failure and abort clean up the crowd. |
| EndlessWaves | Infinite waves of five enemies, spawned together outside the main camera frustum on reachable NavMesh positions. The next wave starts after ten scaled seconds or immediately when the newest wave is cleared. Earlier survivors stay alive. If no valid offscreen batch fits, spawning retries every second. Inspector settings control count, interval and clearance. Failure, abort, completion or disable clean up the run. |
| MobTest | Manual mob sandbox. Select type, level and summon count and press Spawn Mob in the selected mission Inspector during Play Mode. Spawns on nearby reachable NavMesh (default 5 units), uses configured combat groups, and supports Clear Test Mobs. End, failure, completion, disable and mission switching clean up spawned mobs. Mission settings and spawn buttons appear on the MobTest component Inspector; MissionManager provides mission selection and lifecycle controls. |
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
- Player and enemy animation controllers. Animated enemies use `AnimatorFrequency.Tick` when managed by the optional animation LOD system, using full elapsed time and disabling root motion during manual evaluation. Tank and boss placeholder prefabs currently have no Animator.
- Player rigging for traversal poses, plus foot placement components and physical hit-reaction tooling.

See [building navigation](building-navmesh.md) and the [foot placement component documentation](../Assets/Scripts/Presentation/Animation/FootPlacement/README.md).

## Supporting systems and prototype foundations

- `CloudSavePersistentStatStorage` can be assigned to `PersistentStatManager` to save authenticated player levels under one Cloud Save key. Authentication is required before loading. Scene setup and live round-trip validation remain pending. See [Cloud Save](cloud-save.md).
- `GameManager` starts anonymous Unity Authentication through its owned `PlayerAuthentication` service. Online callers can await sign-in and read the player ID; gameplay continues if authentication fails. Package resolution and live sign-in validation remain pending. See [authentication](authentication.md).
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

Grouped enemy spawns enter Pursuing through the same group-assignment method as recruited enemies. Exterminate recruits within 15 units, beyond the idle stopping distance of 3 units, so idle enemies can approach into recruitment range.

Player locomotion has a `MovingBackward` Animator bool. Shooting rigging sets it from the aim direction; PlayerAnimationController exposes `SetMovingBackward`, and rigging resets it after the shooting hold delay. The base locomotion state blends between idle, Walk Back and Unarmed Run Back by horizontal speed while the bool is active, then returns to the existing forward locomotion tree.

Shooting arm IK keeps the hand target at 88% of arm reach to preserve elbow bend. Each hand’s rotation is calibrated independently from its rest-pose bone axes in Awake; Shooting Hand Rotation Offset is an additional local adjustment. Targets follow the shoulder during the shooting hold and blend-out.

Player shooting blends in over 0.12 seconds and releases over 0.18 seconds after a 0.25-second hold. Aim changes are smoothed with scaled time. The Aim Rig evaluates chest before head, with weighted yaw up to 55 degrees for the chest and a 40-degree head limit, using world up to reduce roll. Arm IK evaluates after the torso/head rig; backward shooting keeps its reversed arm selection.

Gameplay feedback uses the scene-owned EffectsManager and disposable player/combat/progression listeners, with editable Feel players for 12 triggers. Gameplay only publishes notifications. See [gameplay-effects.md](gameplay-effects.md) for setup and extension details.
