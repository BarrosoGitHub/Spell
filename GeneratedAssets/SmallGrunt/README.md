# Small Grunt test enemy

Unity asset: `Assets/Models/Enemy/SmallGrunt/SmallGrunt.fbx`

A small silver alien robot inspired by the supplied reference: split pointed helmet, swept armor plates, exposed mechanical joints, orange energy conduits, dorsal fins, and an integrated forearm cannon. Built as a simplified gameplay test model; detailed surface wear and baked PBR textures are not included.

- 1.40 m tall, approximately 0.81 m wide including arms and cannon.
- Ground-level pivot; metre units; exported with Unity-compatible Y-up/-Z-forward FBX settings.
- One skinned mesh, 5,310 triangles, 11 bones, six solid-colour materials.
- Idle (2 s), Walk (1 s, in place), and Attack (0.8 s) at 30 fps.
- Mechanical rigid skinning; intended for a Generic rig, not Humanoid retargeting.

## Unity setup

Select the FBX and set Rig > Animation Type to Generic. In Animation, enable Loop Time for Idle and Walk; leave Attack non-looping. Blender may prefix the imported clip names with the rig name. Assign the clips to an Animator Controller as appropriate.

For a quick visual test, drag the FBX into a scene. For gameplay, add it as the visual child of an enemy prefab and align its feet to ground level. The existing normal enemy uses a 2 m collider and NavMesh agent height, so adjust those to the smaller model when integrating it. Keep gameplay scripts and colliders on the enemy root. Existing animation controllers may require remapping to this rig's clips.

Material base colours are stored in the FBX. If your Unity render pipeline requires it, extract the materials and select the project's shader. Enable orange emission on Energy_Orange to reproduce the illuminated face, joints, and cannon rails. No texture files are required. The preview includes Blender studio lighting and a floor that are not exported. The cannon is currently part of the skinned visual mesh, not a functional weapon component.

## Source and checks

`SmallGrunt.blend` is the editable source, including preview setup. `create_small_grunt.py` and `reference_geometry.py` regenerate the asset using Blender 4.4. `validation.json` records a successful FBX re-import check for geometry, scale, skin weights, skeleton, and animation clip count. Unity import and gameplay have not been verified.
