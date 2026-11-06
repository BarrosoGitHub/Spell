# Exposed chassis variant — 2026-10-01

Created a separate textured Meshy model using `Meshy_reference_chassis.png`, derived from the unarmed reference with exterior armor removed. The finished preview visibly shows an exposed mechanical frame, cables, pistons, joints, and orange power details, with complete empty hands.

Settings: Meshy 7.1 High Detail, standard resolution, textures enabled, image enhancement disabled, CC BY 4.0. Cost: 30 credits; balance changed from 1310 to 1280.

The newest textured chassis card is selected in https://www.meshy.ai/pt-PT/workspace. Meshy reports 4,887,304 triangles and 2,648,705 vertices. Preview: `Meshy_chassis_preview.png`.

This is a visual base for destructible armor, not a ready-to-use modular armor setup. Because the armored model and chassis were independently generated, exact geometric alignment is not guaranteed. For Unity, download the models when export access is available, optimize the detailed sources, align to a shared skeleton, extract/rebuild individual armor plates as separate meshes, and fit them over this chassis. Plate colliders, health, detachment, and damage routing still require implementation.

No new FBX has been downloaded or imported. The previously observed account restriction requires Pro for model downloads. Earlier local FBX files remain procedural placeholders.
