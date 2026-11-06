# Industrial Courtyard greybox

Open `Assets/Scenes/Industrial Courtyard Greybox.unity` in Unity. This is a standalone environment scene, with an overview camera and lighting. It does not contain the gameplay managers, a playable character, or a baked NavMesh. Spawn objects are placement markers only.

The 64 × 64 metre blockout contains a central combat court, low cover (1.1 metres), sightline blockers, an open-roof west warehouse, ground-level flank lanes, an east loading terrace, and a north gallery with an underpass. The elevated route is 3 metres high; stair risers are 0.2 metres. The floor grid is spaced at 8 metres. Orange marks the player start, approach and objective. The southern traversal samples are 1, 2 and 3 metres tall, alongside a 1.8 metre human-scale block.

All 75 geometric objects retain their ProBuilderMesh components for vertex, edge and face editing. Solid pieces have mesh colliders; floor markings do not. Mesh assets and four URP materials live in `Assets/Greybox/IndustrialCourtyard`. Hierarchy groups separate perimeter, combat, warehouse, terrace, gallery and markers.

Use the scene as an environment source for a gameplay test scene; place the existing player at the PLAYER START marker (0, 0.1, -26), then configure the project's navigation and spawning systems as appropriate. Combat balance and character traversal still need playtesting.

`Tools > Survival > Create ProBuilder Courtyard` creates the scene only when it does not already exist; otherwise it selects the existing asset without replacing edits. The builder is in `Assets/Editor/SurvivalGreyboxBuilder.cs`. The rendered overview is `GeneratedAssets/Greybox/IndustrialCourtyard.png`.
