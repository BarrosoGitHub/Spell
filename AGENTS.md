# Project guidance

## Priorities

- Prioritize runtime performance, class decoupling, and readability. Preserve gameplay behavior unless the task calls for changing it.
- Prefer the simplest design that meets the requirements. Optimize demonstrated bottlenecks; explain tradeoffs when an optimization adds complexity or retained memory.
- Make focused changes. Avoid unrelated refactors, formatting churn, new frameworks, or speculative abstractions.

## Project context

This is a Unity third-person action prototype with traversal, shooting, enemy encounters, missions, and destructible geometry. Use `ProjectSettings/ProjectVersion.txt` and `Packages/manifest.json` as the source of truth for the editor version and dependencies.

- `Assets/Scripts/Gameplay`: actors, actions, combat, equipment, and interactions.
- `Assets/Scripts/Game`: game flow, managers, and missions.
- `Assets/Scripts/Infrastructure`: pooling, enemy spawning, navigation, input, networking, interpolation, chunks, and climbing holds.
- `Assets/Scripts/Presentation`: animation, rigging, camera, audio, and UI.
- `docs/`: feature descriptions and subsystem design notes. Start with `docs/game-features.md` and read the documentation relevant to the task. Verify claims against current code; documentation may lag implementation.

Follow the existing organization for new code. These folders are a guide for responsibilities, not proof that dependency boundaries are already enforced. Do not reorganize the project as a side effect of a feature change.

## Class responsibilities and dependencies

- Give each class a cohesive responsibility. Keep game rules separate from input handling, Unity lifecycle wiring, presentation, and resource management where practical.
- Prefer composition over extending controller inheritance or adding unrelated responsibilities to managers. Managers should coordinate collaborators rather than contain every subsystem's behavior.
- Make dependencies explicit through serialized references or initialization methods for Unity components, and constructors for plain C# classes. Resolve scene dependencies at setup boundaries rather than repeatedly discovering them during gameplay.
- Avoid introducing new global mutable state, singletons, or service locators. When integrating with existing globals such as `GameManager` or `Pooler`, contain access at the owning setup or orchestration boundary where practical.
- Introduce small interfaces when they provide a meaningful boundary, alternate implementation, or useful test seam. Do not create an interface for every class or add a dependency injection framework by default.
- Prefer direct method calls for commands and typed events for notifications. Gameplay state should not depend on UI, audio, or animation consumers to remain correct. Avoid a global event bus for local communication.
- Keep mutable collections and state owned by one class. Expose only the operations and read access consumers need. Prefer C# `event` for new notifications so only their owner can raise them.
- Pair subscriptions with unsubscriptions at matching lifecycle boundaries. Account for disable, destruction, scene changes, and repeated pool reuse; an object must not retain subscriptions or callbacks from a previous spawn.
- Make ownership of coroutines, interpolation sequences, jobs, and pooled objects explicit. Cancel or complete outstanding work at the appropriate lifecycle boundary, and prevent callbacks from acting on despawned or replaced objects.

## Runtime performance

- Inspect the frequency and scale of changed code: per frame, physics tick, enemy, chunk, or hit. Avoid accidental quadratic work and repeated full-scene or full-collection scans.
- Keep steady-state hot paths allocation-free where practical. Avoid LINQ, closures, boxing, temporary collections, repeated string formatting, and allocating physics queries inside frequently executed loops. Initialization and editor code may favor clearer allocating APIs.
- Cache stable component references and reuse buffers with sensible capacities. Handle full query buffers explicitly so non-allocating queries do not silently lose gameplay results. Bound caches and release retained resources when their owner ends.
- Reuse the existing pooling infrastructure for frequently spawned enemies, holds, and effects where appropriate. Reset all per-use state on reuse, including targets, timers, events, physics state, and visual state. Check activation order when initialization must happen before `OnEnable`.
- Prefer notifications or scheduled work over polling unchanged state. Budget or spread expensive AI, navigation, geometry, and hold updates when their timing allows it; do not change simulation behavior silently.
- Preserve the existing Job System/Burst hold rebuild path. Keep unsupported Unity object APIs on the main thread, respect job dependencies, and dispose native allocations safely. Add job parallelism only when the workload justifies its scheduling and synchronization cost.
- Use `Update`, `FixedUpdate`, and `LateUpdate` according to the work's timing requirements. Be explicit about scaled versus unscaled time, especially for pause, slow motion, cooldowns, and recovery.
- Avoid repeated material instantiation, mesh allocation, and diagnostic logging in hot paths. Preserve shared assets and batch rendering behavior when modifying visual effects.
- For performance claims, compare representative before/after Unity Profiler captures under the same conditions. Report CPU time, allocations, memory, or frame spikes relevant to the change. Do not claim a speedup from code inspection alone.

## Readability and C# style

- Follow the surrounding C# formatting and naming conventions. Use descriptive names, explicit access modifiers, small cohesive methods, and guard clauses to keep control flow easy to follow.
- Prefer straightforward control flow over clever expressions, deeply nested conditions, or boolean arguments whose meaning is unclear at the call site.
- Name meaningful constants and expose designer-tunable settings through appropriate serialized fields. Include units in names or tooltips where ambiguity matters.
- Prefer private serialized fields and controlled properties for new component state. Preserve existing serialized names and public APIs unless their migration is part of the task.
- Document intent, ownership, invariants, and non-obvious algorithm choices. Avoid comments that repeat the code or speculative TODOs unrelated to the task.
- Validate required configuration at a clear boundary and provide actionable diagnostics. Do not hide invalid setup with broad exception handlers or endless null checks in every frame.
- Extract plain C# logic when it clarifies rules or enables useful tests; do not split simple behavior into many tiny classes just to reduce file length.

## Unity assets and repository safety

- Preserve existing user changes, including staged and unstaged edits. Inspect the working tree before editing and keep the task's diff focused.
- Preserve `.meta` files and GUIDs. Move or rename Unity assets with their metadata and check serialized references. Use `FormerlySerializedAs` for serialized field renames where appropriate; class, namespace, and assembly changes require separate reference checks.
- Keep editor-only code in editor assemblies or `Editor` folders, or behind `UNITY_EDITOR`. Runtime code must not acquire an unconditional `UnityEditor` dependency.
- Do not hand-edit generated `.csproj`/solution files or build caches such as `Library`, `Temp`, and `obj`. For input changes, edit `Assets/Scripts/Infrastructure/Input System/PlayerControls.inputactions` and regenerate its C# wrapper through Unity.
- Avoid modifying vendored packages or Starter Assets when project-owned code can solve the problem. Change dependencies only when the task requires it.
- Do not delete assets based only on missing text references. Consider scenes, prefabs, GUIDs, Resources loading, reflection, and authoring references. Preserve recovery assets and reference variants.

## Validation and delivery

- Run checks proportionate to the change. For behavior fixes or substantial logic changes, add focused regression coverage where useful; do not create tests that merely duplicate the implementation or test documentation edits.
- Use Unity compilation and relevant Edit Mode or Play Mode tests when available. `dotnet build Assembly-CSharp.csproj --verbosity quiet --nologo` can provide a supplemental C# check when generated project files and the SDK are available; it does not verify asset imports, scene wiring, player builds, or gameplay.
- For lifecycle changes, verify repeated spawn/despawn, enable/disable, cancellation, and scene teardown as applicable. For missions, check completion, failure, abort, and restart paths affected by the change.
- For geometry changes, run the relevant existing validation tools, including shell-edge or debris checks, and verify affected climbing holds and collision behavior.
- Check serialized references and prefab/scene wiring when changing component contracts. Update relevant subsystem documentation when behavior or architecture changes.
- Report what changed, what was verified, and any remaining limitations. Clearly distinguish static inspection, compilation, Play Mode testing, and measured performance. If Unity checks could not run, say so.

## Project character art

For new character concepts and image-to-3D references in this project, follow `docs/character-style.md` and use `GeneratedAssets/TechwearCharacter/Techwear_balanced_dark.png` as the approved visual reference. Apply this default unless the user requests a different direction. Preserve existing reference variants.
