# Performance audit

Reviewed 2026-11-05 against the current working tree, including existing staged changes.

## Scope and confidence

Static audit of the project-owned C# code under `Assets/Scripts` (93 files, approximately 12,400 lines), five project shaders, and relevant prefab/scene settings. Reviewed runtime call chains in detail for actors, missions, pooling, destruction, holds, animation, combat, and navigation. Generated input code, easing functions, data-only types, and editor validation tools were screened rather than treated as runtime optimization targets. Starter Assets runtime scripts received a pattern scan; third-party package internals, imported art, and Unity engine implementation were not exhaustively audited.

No Unity Profiler captures, Memory Profiler snapshots, Play Mode tests, player builds, or GPU captures were run. The mechanisms below are visible in source; their actual millisecond/byte impact and scene prevalence require measurement. Priorities reflect likely impact and correctness, not measured rankings. Runtime code was not changed.

## Fix first: lifecycle and retained resources

### 1. Runtime chunk meshes have no explicit cleanup — high priority

**Evidence:** `Assets/Scripts/Infrastructure/Chunks/ChunkGenerator.cs:1310` creates a mesh for every runtime chunk. `OnDestroy` at line 198 only invokes `Destroying`; it never destroys the owned chunk meshes. The explicit mesh destruction elsewhere in this file covers only the temporary source mesh. Debris, by comparison, explicitly destroys its owned mesh.

**Impact:** Destroying/replacing destructible objects during a session can leave native mesh allocations awaiting unused-resource cleanup. Destroying the child GameObjects is not an explicit disposal path for these separately created meshes.

**Recommendation:** Destroy every owned chunk mesh during teardown, preserving imported/shared source assets. Verify repeated create/destroy cycles with native Mesh counts and memory snapshots, without relying on scene-wide unused-resource collection to hide retention.

### 2. Mission and enemy cleanup can leave live work behind — high priority

**Update 2026-11-06:** Applied a small fix using the existing available-enemy list and cleanup methods: register ungrouped spawns, honor group despawning, clean up the previous encounter when Exterminate starts, clean up on destruction, and reject duplicate starts. Completion leaves enemies alive. Recruitment and manager lifecycle behavior are unchanged. The evidence below records the pre-fix finding.

**Evidence:** `Game/Missions/MissionTypes/Exterminate.cs:30` completes a mission without clearing its enemy manager. Its reset/start path can create another group and another batch while the previous group remains registered. `Infrastructure/EnemySystem/EnemyManager.cs:229` accepts `despawn` but never uses it, so `RemoveEnemyGroup(..., true)` only detaches enemies. The type/position `SpawnEnemy` overload at line 66 does not add new enemies to `availableControllers`; enemies spawned by `EnemySetup` and never recruited can therefore be missed by bulk cleanup. The manager has no teardown cleanup of its own.

**Impact:** Repeated missions or removal of encounter owners can leave enemies simulating and groups querying, sorting, and attacking after their intended lifetime. This is an accumulating workload, not just a small allocation issue.

**Recommendation:** Track all spawned enemies separately from group membership. Define cleanup for completion, failure, abort, replacement, and owner destruction; honor the despawn flag. Verify active enemy counts and coroutine ownership return to baseline after repeated mission cycles. If completed encounters intentionally retain enemies, transfer ownership explicitly.

### 3. Wave loops multiply on repeated entry into Playing — conditional high priority

**Evidence:** `Game/Managers/Waves/WaveSystemManager.cs:116` starts an endless wave coroutine every time Playing is entered. No handle prevents duplicates; MainMenu and Paused do not stop it, and `OnDisable` only unsubscribes from the event.

**Impact:** If this prototype component is wired into game-state transitions, repeated pause/resume or menu/play transitions can accumulate wave loops and completion timers. Disabling only the component does not provide explicit cancellation.

**Recommendation:** Own one wave-loop handle and its completion work, and stop/reset it at defined boundaries. Verify multiple game-state cycles produce one wave progression stream. Lower priority until this unfinished system is used.

## Largest runtime scaling risks

### 4. Every active enemy can run a full dynamic ragdoll — high priority

**Evidence:** `Presentation/Animation/Ragdoll/HitRagdollManager.cs:53` forces AlwaysAnimate. Its FixedUpdate at line 93 updates every limb; line 101 makes any strength below 1 dynamic. `Controllers/HitRagdollManager.Setup.cs:40` constructs up to 11 bodies with joints, using 12 solver iterations and 4 velocity iterations per body. `Assets/Prefab/Enemies/Enemy_normal.prefab:538` sets overall strength to **0.877**, with both region multipliers at 1.

**Impact:** Enemies using that prefab setting simulate physical limbs continuously, even outside hit reactions, with no distance or visibility policy. This multiplies physics, animation, and transform work with crowd size. Strength exactly 1 removes dynamic simulation but still executes the pose loops and keeps animation active.

**Recommendation:** Profile crowd sizes with the current physical behavior, then investigate distance-based simulation quality and animation-only states where acceptable. Preserve hitbox accuracy and recovery behavior; changing strength globally changes the intended look and is not a free optimization.

### 5. Destruction performs the full rebuild pipeline in one frame — high priority

**Evidence:** `Infrastructure/Chunks/ChunkGenerator.cs:435` rebuilds dirty chunks and then generates debris synchronously. `RebuildDirtyChunks` at line 1387 uploads meshes, reassigns MeshColliders, calls `Physics.SyncTransforms`, and invokes hold rebuilding before returning. `Infrastructure/Edges/EdgeSpawner.cs:158` rebuilds all affected hold chunks. `ShellEdgeJobs.cs:61` and `:74` immediately complete scheduled jobs, once per chunk.

**Impact:** Geometry generation, collision updates, hold reconstruction, and debris creation converge on the damage frame. Burst accelerates a portion of this work but does not make the caller asynchronous. A large cut or multiple affected objects can cause a frame spike.

**Recommendation:** Instrument these phases separately. Coalesce dirty work, reuse scratch buffers, and consider batching independent chunk jobs before completion. Defer cosmetic debris/holds where safe; collision and grab validity need an explicit consistency policy if work crosses frames.

### 6. Debris has no active-count or per-frame creation budget — high priority

**Evidence:** `Infrastructure/Chunks/DebrisManager.cs:51` creates fresh meshes, GameObjects, renderers, colliders, and rigidbodies for each piece. Ground collisions call `Break` synchronously, which creates descendants and resets their lifetime. `Assets/Scenes/Test 1.unity:1818` configures **44 initial pieces**, **5 pieces per break**, **3 break levels**, and **25.8 seconds** lifetime.

**Impact:** Repeated damage and concurrent impacts can produce bursts of allocation and physics/rendering work. Fragment counts are limited by available cells and depth, so 44 × 5³ is only a theoretical branching bound, not a predicted piece count. There is no global budget across hits or managers.

**Recommendation:** Set an explicit global active-body budget, a per-frame fracture budget, and an extraction expiry inherited by descendants. Pool reusable object components if profiling justifies it; mesh geometry and owned-resource disposal still need separate management.

### 7. Pursuit requests a path every physics tick — medium/high priority

**Evidence:** `Gameplay/Actors/Enemies/EnemyController.cs:106` calls `SetDestination` on every pursuing FixedUpdate regardless of target movement or pending paths. The project timestep is 0.02 seconds: 50 requests per pursuing enemy per simulated second under normal timing.

**Impact:** At 100 pursuing enemies this is 5,000 destination requests per second, not necessarily 5,000 completed path calculations. The default encounter group is smaller, so this is principally a scaling risk.

**Recommendation:** Repath on a staggered interval and meaningful target displacement, with explicit handling for invalid/stale paths and navigation changes. Compare navigation CPU cost and pursuit responsiveness.

### 8. Enemy selection repeatedly allocates and sorts broad queries — medium priority

**Evidence:** `Infrastructure/EnemySystem/EnemyManager.cs:184` creates several LINQ results and temporary sets/lists every group refresh, normally twice per second. `GetClosestEnemyControllers` at line 218 performs an allocating overlap without an enemy layer mask, resolves components, deduplicates, sorts, and materializes a list. Attack selection at line 124 can sort every 0.1 seconds while waiting for eligible attackers.

**Impact:** Periodic GC pressure and CPU work increase with group count and nearby colliders. Ragdoll colliders, holds, and debris can enlarge the overlap results even though they are rejected later.

**Recommendation:** Use an enemy registry or filtered reusable overlap buffer, reusable selection collections, and bounded nearest-candidate selection when only a few enemies are needed. Preserve distinct-enemy semantics and handle buffer saturation explicitly. Stagger group refreshes.

### 9. Aiming allocates and queries before shot eligibility is known — medium priority

**Evidence:** `Gameplay/Actors/Player/PlayerController.cs:379` resolves aim every rendered frame while attack is held. `Infrastructure/Helpers/CalculationsHelper.cs:23` uses `RaycastAll`; the rifle's cooldown/reload rejection occurs later in `AutomaticRifle.TryExecuteAttack`. Accepted shots perform another `RaycastAll` at `Gameplay/Combat/Weapons/Ranged/AutomaticRifle.cs:73`.

**Impact:** Holding fire pays for aim queries and result arrays even during cooldown and reload. Dense scenes increase per-hit hierarchy/component checks.

**Recommendation:** Separate non-mutating shot readiness from shot execution, or maintain a reusable aim query if continuous aiming is needed for presentation. Use reusable hit buffers with overflow handling and cache stable camera references. Preserve camera aiming versus muzzle obstruction behavior.

## Holds, startup, and allocation bursts

### 10. Debug visualization performs gameplay-time work — medium priority

**Evidence:** `Gameplay/Actors/Player/PlayerController.cs:163` calls hold highlighting each frame. The airborne path at line 438 allocates an overlap array, dynamically adds highlight components, and reapplies property blocks even when flags are unchanged (`Infrastructure/Edges/HoldSelectionDebug.cs:26`). Parkour can perform a second nearby-hold query. `Gameplay/Actors/Enemies/EnemyController.cs:28` automatically adds `EnemyPursuitIndicator`; its `OnGUI` draws two labels per pursuing enemy on repaint without a development-only gate.

**Impact:** Debug-only work adds physics queries, allocations, renderer updates, and IMGUI overhead to ordinary gameplay. Existing early-outs reduce this cost but do not remove it when the debug conditions are active.

**Recommendation:** Put debug visuals behind an explicit development toggle, update colors only on changes, and share hold-query results where semantics match. Prefer one centralized overlay if many enemy labels are needed.

### 11. Hold loading scans the whole registered world — medium priority

**Evidence:** `Infrastructure/Edges/NearbyHoldChunkLoader.cs:22` refreshes every 0.2 seconds. `Infrastructure/Edges/EdgeSpawner.cs:230` checks every registered grid chunk and recomputes world bounds even when the player is stationary. Non-grid objects take the eager spawn path at line 98; proximity loading skips them entirely.

**Impact:** Refresh cost scales with total world chunks, not nearby chunks. Boundary oscillation can repeatedly activate/deactivate whole chunks. Static non-grid geometry can keep all its hold GameObjects active regardless of distance.

**Recommendation:** Cache bounds for unmoving grids, use spatial candidate lookup and load/unload hysteresis, and provide invalidation for moved/new geometry. Extend proximity handling to non-grid holds if they are numerous.

### 12. Destruction startup bakes everything synchronously — medium/high startup priority

**Evidence:** `Infrastructure/Chunks/ChunkGenerator.cs:351` clones the source mesh, clips geometry, classifies cells, bakes caps, creates chunks, and cooks their colliders in Awake. Grid storage at line 552 scales with volume divided by cellSize³. Scenes use 0.1-unit cells in several destructible objects. `EdgeSpawner.GenerateHoldData` also calculates original mesh edges for grid objects, then `BuildForObject` discards those supplied edges and builds current shell holds instead.

**Impact:** Loading/instantiating many destructible objects can stall startup. Halving cell size grows the dense cell count by approximately eight times for fixed bounds. Original-edge work is redundant for the grid-specific hold path.

**Recommendation:** Consider serialized prebakes for immutable source geometry, staged initialization where gameplay permits, and bypassing original-edge calculation for ready grids. Measure loading time separately from steady gameplay.

### 13. Hold rebuilds allocate fresh managed geometry and placement data — medium priority

**Evidence:** `Infrastructure/Chunks/ChunkGenerator.cs:171` creates a new nested RuntimeChunk scratch object and its lists for each shell build. `Infrastructure/Edges/EdgeSpawner.cs:158` creates fresh lists, sets, chunk/cell containers, and one HoldPlacement object per planned hold. Native job buffers are reused, but the surrounding managed pipeline is not.

**Impact:** Repeated destruction can generate garbage proportional to rebuilt geometry and hold count. This is distinct from the computation cost in finding 5. The unrelated standalone `RuntimeChunk.cs` with preallocated lists is not the nested type used here.

**Recommendation:** Reuse shell scratch storage and batch-level collections. Consider compact placement records and preserving unaffected data after measuring allocation call stacks. Retained capacity must remain bounded.

### 14. Pool sizes do not prewarm objects — medium priority

**Evidence:** `Infrastructure/Pooler/Pooler.cs:27` passes `size` as ObjectPool default capacity and only instantiates from the creation callback. There is no prewarm. First encounters and first hold loading therefore create objects on demand. `maxSize` limits inactive retention, not simultaneous active objects. There is also no explicit pool disposal/static reset at owner teardown.

**Impact:** First-use spikes remain despite pooling; repeatedly exceeding inactive retention can cause instantiate/destroy churn. The pool is not an active gameplay budget.

**Recommendation:** Rename the capacity setting for clarity or implement measured prewarming during loading. To create N distinct entries, retain N checked-out objects before returning them. Size retention from observed peaks and define ownership/teardown separately from active-object limits.

### 15. Each interpolation creates and destroys a component — medium/low priority

**Evidence:** `Infrastructure/Interpolator/Interpolate.cs:101` and the other extension methods add a MonoBehaviour, capture delegates, and start a coroutine for each interpolation; completion destroys it. Traversal rigging can start many of these together. `RangedWeapon.cs:26` uses the same mechanism purely as a reload timer.

**Impact:** Bursty component/native-object churn and managed allocations during actions. This is more relevant to many concurrent enemies or rapid traversal than to occasional single-player effects.

**Recommendation:** If measured, use a reusable interpolation runner with explicit cancellation and plain timestamp timers where no interpolation is needed. Preserve the current deferred configuration and cancellation semantics.

## Lower-priority or conditional observations

- `Infrastructure/Navigation/BuildingNavMeshBaker.cs:297` obtains and scans whole-world NavMesh triangulation for each building's link generation. City baking can scale poorly as building count grows. Inspected `Test 1` entries have `bakeOnStart: 0`, so prioritize this as editor tooling/loading work, not steady-frame gameplay.
- The transparent range-indicator shaders shade the full submitted geometry, including zero-alpha regions. Many overlapping indicators can create GPU overdraw. Verify screen coverage and draw calls before changing geometry/shaders; no GPU bottleneck was established.
- Player and enemy animation classes register state delegates in Start without removing those state delegates. They do not multiply on ordinary pool reuse because Start runs once, but disabling/replacing a presentation component alone can leave its callbacks active. Pair these subscriptions if independent presentation lifetimes are supported.
- `AudioManager.Play` allocates a filtered array on each call, but its registration methods are currently empty and no active call chain was established. Do not prioritize it above confirmed runtime paths.
- InteractionContext and DamageResult objects allocate per processed hit; investigate only if combat allocation measurements justify changing their ownership/value semantics.
- Mission reflection discovery occurs at initialization, not every frame. Existing foot IK uses two non-allocating single-hit probes, bullet effects have a 128-effect cap and reuse objects, and native hold buffers are retained/disposed explicitly. These are not immediate rewrite targets.

## Suggested verification order

1. Repeat mission start/completion/reset and encounter teardown; record active enemies, groups, and wave-loop counts. Repeat destruction-object creation/removal and record native Mesh counts.
2. Capture identical crowd scenarios at 10, 50, and 100 active enemies; separate animation, ragdoll physics, navigation, and group-selection cost. Compare visible and offscreen crowds.
3. Capture a large cut and repeated cuts in `Test 1`; separate picking, mesh build/upload, collider update, hold jobs, managed hold planning, and debris creation. Track worst-frame CPU, GC allocations, active rigidbodies, and memory after expiry.
4. Hold fire during normal firing and reload, then traverse dense holds with debug visualization enabled/disabled. Compare query counts and allocation call stacks.
5. Record startup separately and inspect GPU overdraw/draw calls in representative combat. Use the same player build, scene, hardware, quality settings, and simulation conditions for before/after comparisons.

API semantics checked against [Unity SetDestination documentation](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/ai/navmeshagent/setdestination), [Unity's ObjectPool reference source](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Runtime/Export/ObjectPool/ObjectPools.cs), and [Unity mesh ownership documentation](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/meshfilter/mesh). Findings and prioritization are based on this project's code, not benchmark results.
