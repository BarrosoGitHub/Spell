# Nature Square Arena

Scene: `Assets/Scenes/Nature Square Arena.unity`.

A flat 64 × 64 metre arena using Polytope Studio's **Low Poly Environment – Nature Free – LOWPOLY MEDIEVAL FANTASY SERIES**. The supplied URP 17 shader package is imported for this URP project. The scene retains the player, camera, managers, enemy pool and UI from the existing gameplay setup. Earlier environment roots are inactive in this scene.

The perimeter has 64 wooden fence sections and four continuous invisible BoxCollider limits. Limits extend well above the fence and below the ground, including overlapping corners. Border trees, shrubs and flowers frame an open combat area with four small rock/log clusters and a northern standing-stone landmark. The ground height is uniformly zero; paths are painted surfaces, not raised geometry.

Environment props remain on Default so the existing Ground-layer climbing scanner does not attempt to read their statically batched visual meshes. Existing prefab colliders participate in navigation and movement collision; the terrain and boundary colliders use Ground. This is a flat combat arena, not a newly authored climbing/destruction course.

## Color style

The woodland palette uses moss/sage foliage, warm stone and wood, charcoal/olive player tones and muted rust-red enemy tones. Environment material variants live in `Assets/Maps/NatureSquareArena/Palette`; vendor material assets remain available. The existing player and normal-enemy materials receive restrained base tints and reduced metallic/smoothness values, retaining their original texture maps. A scene-owned global URP volume reduces saturation and adds slight warm grading across characters and environment. Enemy hues remain distinct from foliage for combat readability. Subtle bloom (0.65 intensity, 0.65 threshold, 0.55 scatter, four iterations) softens bright highlights, and a light vignette (0.3 intensity, 0.5 smoothness) frames the arena.

## Editing and verification

The editor builder is `Assets/Scripts/Infrastructure/Environment/Editor/NatureArenaBuilder.cs`. `Tools > Environment > Create Nature Square Arena` preserves an existing arena instead of overwriting it. Existing maps can be edited directly in the hierarchy. Ground painting, woodland palette, overview rendering and navigation/boundary validation have separate menu commands.

Navigation was baked using active map-child physics colliders. Editor validation checks complete paths from player start to all five current Exterminate spawn positions and raycast blocking at all four limits. These are targeted checks, not an exhaustive traversal or combat test. Runtime mission defects described in the deep review remain separate from map construction. Standalone build and sustained performance testing have not been run.

## Arena camera

The scene's Arena Camera uses the existing Cinemachine orbital follow and rotation composer, tracking the player directly in world space. Default framing is 55° downward, 14 metres from the target, with a 45° perspective field of view and short positional damping. Middle-mouse drag changes yaw; the mouse wheel smoothly zooms from 14 to 24 metres. Pitch stays fixed for consistent arena readability. Right-mouse slow motion and ordinary mouse aiming retain their existing inputs. `ArenaCameraInput` owns only orbit/zoom input; Cinemachine continues to own follow and framing. The former always-on input-axis controller is disabled for this camera.
