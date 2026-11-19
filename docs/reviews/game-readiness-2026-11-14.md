# Deep code review and game completion checklist

Review date: 2026-11-14. Scope: current working tree, including staged, unstaged, and untracked project scripts. Unity 6000.6.2f1; dependency versions come from `Packages/manifest.json`.

## Executive assessment

The project has substantial prototype systems: third-person traversal, pooled enemies, grouped combat, several mission implementations, weapon effects, equipment rewards, destructible geometry, rebuilding climbing holds, navigation authoring, rigging, and animation scheduling. The biggest gap is their integration into a reliable complete run. More assets or enemy types will not resolve mission completion, interruption, progression, and lifecycle defects.

This review assumes a single-player action game with repeatable missions and one polished arena as the first shipping target. Campaign, multiplayer, procedural worlds, and permanent progression are scope choices, not mandatory missing features.

This is a source review, not a certification of the entire game. No fresh Unity compilation, Play Mode session, standalone build, geometry validator execution, or Profiler capture was performed for this report. Serialized scene configuration and imported animation behavior still require runtime verification. Performance observations below are candidates for measurement, not measured speedups or proven bottlenecks. No gameplay code was changed by this review.

Paths below are repository-relative; line references describe the reviewed snapshot and may move.

## Confirmed findings, ordered by impact

### R01 — P1: Failed mission startup can dereference a cleared player

**Source:** `Assets/Scripts/Game/Missions/MissionManager.cs`, `StartMission` (line 50), `HandleMissionChangedState`, `UnsubscribeFromPlayer`.

The manager captures `subscribedPlayer`, calls the mission's start method, then heals through `subscribedPlayer.entity`. Exterminate can fail synchronously when no NavMesh is found. The failure event unsubscribes and clears that same field before startup returns, so the heal throws. A missing player also reaches this dereference. The method returns success without establishing that the mission entered Running.

**Reproduce:** Start Exterminate without a NavMesh within 15 units of origin; also test starting without a player and starting a finished mission without resetting it.

**Fix direction:** Validate prerequisites before starting, separate the local start participant from subscription bookkeeping, and report actual startup success. Define when reset/healing occurs rather than relying on a hardcoded 300-point heal after callbacks.

### R02 — P1: Exterminate has no victory condition

**Source:** `Assets/Scripts/Game/Missions/MissionTypes/Exterminate.cs`, `StartMission` (line 12) and `CompleteMission`.

Five enemies are spawned, but the mission does not track their deaths or complete when they are defeated. Its completion override also leaves its enemy manager/group work active. Failure and abort do clean up. Killing everything therefore does not finish the objective without an external command.

**Fix direction:** Own the mission's spawned participants and remaining count; complete exactly once when the final participant dies. Make completion, failure, abort, restart, and disable cleanup consistent. Count mission ownership, not every enemy in the scene. Expose progress to the HUD and record meaningful elapsed time.

### R03 — Superseded by removal of the knockdown resource

The resource-driven knockdown and automatic recovery subsystem was removed on 2026-11-19.

### R04 — P1: Player death does not establish a dead gameplay state

**Source:** `Assets/Scripts/Gameplay/Actors/Controller.cs:225`; `Player/PlayerController.cs`, state update callbacks.

The shared death handler clears buffs, notifies subscribers, and cancels the current action. The player has no death override or dead-state transition to stop its movement/parkour callbacks. Individual attack/dodge guards are not a complete player death policy. Mission failure can occur while movement remains active, and cancellation can leave an action state active.

**Fix direction:** Establish one owner for death, disable gameplay commands, restore physical invariants, cancel outstanding work, and reset all required state for retry. Specify how death interacts with slow motion, rewards, and in-flight attacks.

### R05 — P1: Cancelling Grab leaves collision disabled

**Source:** `Assets/Scripts/Gameplay/Actions/Climbing/Grab.cs:14`, `Cancel`.

Preparation disables the main collider and starts positional interpolation. `Cancel` is empty. An interruption that does not enter movingState can retain disabled collision; the action does not own cancellation of its movement interpolation either.

**Fix direction:** Make interruption restore collision and cancel owned movement work without overriding the destination state chosen by death. Check all traversal actions against the same lifecycle contract.

### R06 — P1: Directional melee can hit across floors and through walls

**Source:** `Assets/Scripts/Gameplay/Actions/Attacking/AttackDirectional.cs:69`, `IsTargetInRange`.

The range check uses local X and Z only. It has no vertical bound or obstruction test. Targets above/below the attacker with matching horizontal coordinates pass; an intervening wall also does not invalidate damage. The width is constant across the attack length, so presentation must match that rectangular shape rather than suggest a true cone.

**Fix direction:** Define the intended three-dimensional hit volume and obstruction policy. Test a target on the next floor, behind a thin wall, just outside each boundary, and moving out during preparation.

### R07 — P1: Several offered combat upgrades do not affect the equipped ice weapon

**Source:** `Assets/Scripts/Gameplay/Combat/Weapons/Ranged/IceWeapon.cs:24`; `Assets/Scripts/Gameplay/Interaction/EntityManager.cs:56`; `Assets/Scripts/Presentation/UI/RewardDisplay.cs`, `GetRewardText`.

Ice cooldown uses `WeaponStats.fireRate` alone. Damage is sent as a fixed `DamageOverride`, which bypasses attacker base-damage and critical calculations. Reward UI advertises damage, percentage damage, attack speed, and critical bonuses, but those actor stats do not enter this weapon's attack calculation. Defense still applies to damage received.

**Fix direction:** Choose and document how weapon base stats combine with actor bonuses, then use the same effective-stat rules for firing and damage. Verify each offered reward changes its promised outcome. Balance the requested player speed reduction at the actual firing calculation; changing unrelated action speed cannot achieve it.

### R08 — P1: Held traversal references can outlive pooled holds

**Source:** `Assets/Scripts/Gameplay/Actors/Player/PlayerController.cs`, `closestHold` consumers; `Assets/Scripts/Infrastructure/Edges/EdgeSpawner.cs`, `RemoveCellHolds` (around line 315).

The player retains a GameObject reference to its hold. Rebuilding or unloading holds returns those objects to a pool. A pooled object can remain non-null and later represent another ledge, while traversal continues to use its transform. A null check alone cannot validate that association.

**Fix direction:** Introduce hold invalidation or an identity/version contract and a safe fall/cancel transition. Test destruction of the occupied ledge, nearby streaming unload, and rapid release/reuse during a climb.

### R09 — P2: Global death and kill XP can be emitted more than once

**Source:** `Assets/Scripts/Gameplay/Interaction/EntityManager.cs:122`, `ApplyDamage` and `ControllerDied`.

Entity itself emits its death event only when crossing from positive health to zero. EntityManager instead emits global death whenever the resulting health is zero, including an already-dead target. Its interaction entry point does not reject dead/inactive actors. Current weapon guards reduce exposure but do not enforce correctness for every caller.

**Fix direction:** Award death/XP only on the actual living-to-dead transition. Test two damage calls against the same dead entity and callbacks that despawn targets during the first call.

### R10 — P2: Maximum-health percentage bonuses are not applied

**Source:** `Assets/Scripts/Gameplay/Actors/Entities/Entity.cs`, `UpdateStats`, constructor and `AddCurrentHealth`; `Stats.cs`.

`MaxHealthPerc` is accumulated and displayed, but initialization and health clamping use raw `stats.MaxHealth`. Updating or removing equipment also does not reconcile existing health with a reduced maximum.

**Fix direction:** Centralize effective maximum health and define whether gaining/removing max health preserves the amount, percentage, or deficit. Notify UI when the effective maximum changes.

### R11 — P2: Health bar caches an obsolete maximum

**Source:** `Assets/Scripts/Presentation/UI/HealthBar.cs:12`.

The bar captures maximum health at startup and updates on health events. It does not track later maximum-stat changes and lacks an immediate initial refresh. Equipment changes and pooled reuse can therefore show stale proportions.

**Fix direction:** Refresh both current and effective maximum values at binding/rebinding and on relevant changes; match subscription lifetime to the active binding.

### R12 — P2: Pool activation precedes spawn initialization

**Source:** `Assets/Scripts/Infrastructure/Pooler/Pooler.cs`, object-pool get callback and `SpawnFromPool`; `Assets/Scripts/Infrastructure/EnemySystem/EnemyManager.cs`, spawn setup.

The pool activates objects before assigning their new pose, and enemy reset/configuration happens afterward. OnEnable observers can therefore see the old transform, target, or previous-use state. This is an ordering defect; which visible symptoms occur depends on attached components.

**Fix direction:** Obtain inactive objects, set per-use state and pose, then activate/publish spawn. Validate a reused enemy with subscriptions, animation, physics, buffs, targeting, and navigation enabled.

### R13 — P2: Persistent GameManager retains a scene-bound player

**Source:** `Assets/Scripts/Game/Managers/GameManager.cs`, `Awake`.

The manager survives scene loads but discovers the player only at startup. Loading a replacement gameplay scene can leave a destroyed or obsolete player reference used by missions and enemy setup.

**Fix direction:** Use explicit player registration at scene setup and unregister on teardown. Test gameplay-to-menu-to-gameplay and retry via scene reload.

### R14 — P2: Animation LOD disables hit-ragdoll behavior at every tier

**Source:** `Assets/Scripts/Presentation/Animation/AnimatorFrequency.cs:26`; `AnimationLodManager.cs`.

Manual evaluation disables the attached HitRagdollManager. The scheduler selects manual evaluation for all managed enemies, including the closest tier. Enabling this optimization therefore changes physical hit reactions, not just animation sampling frequency.

**Fix direction:** Decide whether this is an intentional feature tradeoff. If hit reactions are required, coordinate animation/rig/ragdoll ownership instead of removing the reaction component. Test near/far tier changes, disable/re-enable, and despawn during recovery. Do not claim performance benefit without comparable captures.

### R15 — P2: Runtime chunk meshes lack explicit owner teardown

**Source:** `Assets/Scripts/Infrastructure/Chunks/ChunkGenerator.cs:198`, `CreateChunks` (around line 1310).

Each chunk creates a native Mesh and shares it between rendering and collision. The generator's OnDestroy only publishes Destroying; it does not explicitly destroy these meshes. Destroying child GameObjects is not an explicit disposal path for separately created mesh objects.

**Fix direction:** Release owned runtime meshes once, after dependent work is finished. Verify repeated arena creation/destruction in a memory capture. The missing teardown is visible in code; the retained-memory amount has not been measured.

### R16 — P2: Failed and aborted results are missing from the manager result contract

**Source:** `Assets/Scripts/Game/Missions/MissionManager.cs:108`, `HandleMissionChangedState`.

Only Completed populates LastResult and emits the completion notification. Failed and Aborted record end timestamps but do not expose their result through the same manager-level flow. A results screen cannot consistently consume every outcome.

**Fix direction:** Publish a terminal outcome notification for all terminal states while preserving any success-only notification needed by existing consumers. Ensure exactly one result per run.

## Additional integration gaps and decisions

These need explicit product decisions or runtime validation; they should not be presented as proven crashes.

- **Destruction/navigation synchronization:** The source search finds hold rebuild subscribers to `ChunksRebuilt`, but no navigation subscriber. Building/CityNavMeshBaker expose baking operations, not an automatic destruction-to-navigation update pipeline. If enemies must navigate modified geometry, define updates for removed surfaces and links, newly opened passages, stranded agents, and rebuild budgets. Test actual active navigation components before deciding the implementation.
- **Projectile gameplay ownership:** IceProjectileDisplay instantiates and emits the damaging particles. Disabling the display leaves IceWeapon consuming a cooldown without creating a damaging projectile. Separate simulation ownership from optional decoration if effects need independent LOD, settings, or replacement. Current particles explicitly use AlwaysSimulate; do not assume camera culling currently suppresses them.
- **Projectile identity:** All particles call the weapon's shared callback and current `source`. CancelAttack clears that source, invalidating in-flight damage; a subsequent cast assigns it again. Specify whether already-launched attacks should survive cancellation and whether pooled/re-equipped weapons can change attribution.
- **Reward lifetime:** RewardManager queues per controller but clears only when the manager disables, with no participant death/despawn cancellation. RewardDisplay stores one active controller and has no replay/query when re-enabled. Specify who can receive rewards, how overlapping level-ups queue, and what happens on death, retry, and UI disable.
- **XP overflow:** Entity resets XP to zero and adds one level per award, discarding overflow. Decide whether large awards should carry over and grant multiple levels. If so, the notification/reward contract also needs to support that.
- **Equipment slots:** Equip appends items without replacing the occupied slot. This may be intentional stacking, but the slot-based naming suggests inventory semantics that are not enforced. Choose stacking limits and show the actual rule to players.
- **Spawn placement:** Exterminate's five enemies still use a ten-column grid formula, occupying one off-center row. Failed samples collapse onto the same center point. Use authored encounter anchors or deliberately spaced valid positions for a finished arena.
- **Build entry scene:** `ProjectSettings/EditorBuildSettings.asset` enables SampleScene and the Starter Assets Playground; Test 1 is absent. Check the active build profile before calling this a shipping error, then define a deliberate launch scene and scene list.
- **Documentation drift:** `docs/game-features.md` still says Exterminate spawns 100, kill XP is unwired, and reward UI is caller-owned. Current code spawns five, awards XP through EntityManager, and includes RewardDisplay. Do not turn those stale statements into missing-feature claims.

## Architecture and performance assessment

Avoid a broad rewrite. Existing action/state, mission, pool, and hold-rebuild systems can be improved at their boundaries. Prioritize clear ownership: mission owns its participants; controller owns actor state; weapon/combat owns damage; presentation observes; pool owns activation/reuse; geometry owner disposes generated resources.

Review candidates for representative profiling:

| Candidate | Why inspect | Evidence to collect |
| --- | --- | --- |
| Enemy recruitment and attack selection | Repeated physics queries, collection construction and sorting grow with crowds | Main-thread time and GC at 5, 50, and target maximum enemies |
| Hold selection/highlighting | Repeated overlap scans and debug component work | Player Update CPU and allocations in dense geometry |
| Hit logging and pursuit labels | Per-hit formatting/logs and debug overlays are active gameplay work | Release/development comparison under sustained combat |
| Destruction | Mesh rebuilds, collider cooking and job synchronization can create spikes | Largest destruction-frame CPU, job waits, collider costs |
| Hold rebuild jobs | Preserve the existing Burst path; scheduling overhead and immediate completion need measurement | Same geometry/action before and after any optimization |
| Debris | Fragment count, new mesh/rigidbody creation and repeated breakup increase active physics load | Peak object count, allocations, physics time, memory after expiry |
| Animation LOD | Cadence reduction interacts with rigging and ragdolls | CPU plus visible correctness at each tier and during transitions |

Use the same scene, camera, hardware, enemy count, attack sequence, and destruction sequence for comparisons. Report percentile/frame spikes as well as averages. Non-allocating buffers must have an explicit saturation policy; do not silently trade correctness for a lower allocation count.

## What is needed to finish the game

### Milestone 1 — One reliable, complete playable run

- [ ] Deliberate launch flow: title/menu, start mission, objective HUD, success/failure result, retry, return to menu, quit.
- [ ] Exterminate completes automatically after its five owned enemies die; zero duplicate completion/reward events.
- [ ] Missing player/NavMesh/configuration produces an actionable failed start without partial encounter leftovers.
- [ ] Death, recovery, out-of-bounds recovery, and retry have explicit state transitions.
- [ ] Abort, complete, fail, reset, and scene unload cancel encounter coroutines and remove owned enemies.
- [ ] Mission result includes meaningful elapsed time, outcome, and any promised score/rewards.
- [ ] Pause actually controls simulation and UI input; its behavior composes with slow motion, focus loss, and retry.

**Acceptance:** Complete, fail, abort, and restart the encounter repeatedly without stale enemies, stuck player states, duplicate events, or Console exceptions.

### Milestone 2 — Combat and progression that match the UI

- [ ] Define one effective-stat policy for weapon damage, attack speed, criticals, defense, and max health.
- [ ] Every offered upgrade measurably affects the current build, or is clearly restricted to compatible equipment.
- [ ] Set stacking/slot rules, XP overflow rules, reward queue behavior, and run reset/persistence rules.
- [ ] Enemy melee respects walls, floors, telegraph volume, and the execution timing shown by animation.
- [ ] Decide dodge cooldown/invulnerability and communicate the result; neither is automatically required.
- [ ] Provide readable hit, damage, death, level-up, and reward feedback, with a HUD that initializes correctly.
- [ ] Balance firing cadence, damage, enemy health, attack preparation and recovery with timed encounters.

**Acceptance:** For each reward type, compare a baseline and upgraded attack/health result; verify no bonus is cosmetic-only unless explicitly described that way.

### Milestone 3 — Traversal and destruction remain safe during combat

- [ ] Every action restores its owned collider, movement, interpolation, and animation state on interruption.
- [ ] Destroying/unloading/reusing an occupied hold triggers a safe transition.
- [ ] Decide navigation behavior after structural changes and implement invalidation/recovery accordingly.
- [ ] Establish supported destruction sizes, debris budgets, collider fidelity, and lifetime limits.
- [ ] Dispose runtime meshes and native/job resources on teardown.
- [ ] Run existing shell-edge and debris validation tools, then test affected hold and collision behavior in Play Mode.

**Acceptance:** Destroy a ledge while hanging, a floor under an agent, and a wall during combat; neither actor remains permanently stuck or traverses invalid geometry without a defined recovery.

### Milestone 4 — One coherent arena and finished presentation

- [ ] Build a readable arena with encounter locations, traversal routes, boundaries, recovery points, and clear sightlines.
- [ ] Validate floor/cliff texture scale and variation in motion and from gameplay camera distances; source inspection cannot establish visual quality.
- [ ] Verify player/enemy retargeting, root motion policy, weapon sockets, head aim limits, foot placement, and animation transitions with the current meshes.
- [ ] Check rig and hit-reaction behavior at every animation LOD tier.
- [ ] Add purposeful audio for attacks, impacts, movement, enemies, UI and mission outcomes; provide volume controls.
- [ ] Resolve camera obstruction, target visibility, telegraph contrast, effect clutter, and reward readability.
- [ ] Remove or gate debug pursuit labels, hold highlighting, and combat logs in release presentation.
- [ ] Add enemy/encounter variety only after this arena and loop work reliably. Tank/Boss enum entries alone are not finished enemies.

**Acceptance:** A new player can start, understand the objective, read threats, finish/fail, and retry without developer instructions or Inspector controls.

### Milestone 5 — Settings, supported input, and persistence

- [ ] Decide supported platforms and input devices; finish menus and reward selection for each promised device.
- [ ] Add sensitivity, volume, display/quality controls, and remapping where in scope.
- [ ] Include readable text scaling, contrast, and optional camera shake/motion settings where relevant.
- [ ] Decide whether only settings, run progress, or permanent progression persists; implement only the chosen contract.
- [ ] If saves exist, handle versioning, interrupted writes, and reset behavior.
- [ ] Treat multiplayer as a separate scope decision. An ownership component and networking packages are not a complete multiplayer game.

### Milestone 6 — Shipping validation

- [ ] Choose the active build profile and launch scene deliberately; verify all required scenes/assets are included.
- [ ] Clean Unity import and compilation with no missing scripts or broken serialized references.
- [ ] Focused regression coverage for state transitions, death idempotency, stats, mission terminal outcomes, and pooled reuse.
- [ ] Standalone smoke tests on the supported platform, including scene changes, focus changes, resolution/input changes and quitting.
- [ ] Sustained combat/destruction tests with measured CPU, allocations, native memory, and frame spikes against a defined hardware/frame-rate target.
- [ ] Audit third-party asset licenses, credits, build branding, and distribution requirements before release.

## Concrete regression matrix

| Test | Required result |
| --- | --- |
| Start without player or valid NavMesh | Clean rejection/failure, no exception or leftover encounter |
| Kill all five Exterminate enemies | One victory and one complete result; no remaining mission work |
| Hit the same zero-health target twice | One kill notification and one XP award |
| Manual ragdoll activation during movement and traversal | Recovery or death flow completes; collision restored appropriately |
| Die during grab/dodge/recovery/reward choice | Commands stop and retry produces a usable player |
| Abort/complete/fail/retry for 20 cycles | Stable participant counts; no stale callbacks or pending rewards |
| Pool an attacking enemy and reuse it | New pose, target, health, timers, animation and subscriptions are correct |
| Remove the occupied hold and reuse its object | Player does not follow the recycled transform |
| Attack through a wall or onto the next floor | No damage outside the defined hit/obstruction rules |
| Apply each supported stat bonus | Display and measured gameplay agree |
| Toggle LOD and cross each distance tier while hit | Expected rigging and physical reactions persist |
| Repeatedly create/destroy destructible arenas | Runtime mesh/native memory returns toward baseline after cleanup |
| Destroy navigable surfaces and links | Agents repath or recover according to the chosen design |
| Launch standalone, retry, return to menu and start again | Correct entry scene and valid player/manager references |

Run unit/Edit Mode checks for pure rules where useful and Play Mode checks for pooling, physics, navigation, animation and lifecycle behavior. A generated-project `dotnet build` is only a supplemental compilation check, not evidence that scenes, imported assets, or player builds work.

## Recommended order

Fix R01–R08 first, then death/stat/result/pool correctness. Complete Milestone 1 before adding content. Follow with combat progression and traversal/destruction integration, polish one arena, and finish supported settings/build validation. Reassess scope after a complete standalone run is reliable; do not estimate a completion percentage from the number of existing scripts.
