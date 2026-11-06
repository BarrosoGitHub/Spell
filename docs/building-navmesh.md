# Building navigation and climbing

## Automatic links

1. Bake the ground NavMesh using the same agent type as the enemies and building baker.
2. Give the ground and buildings colliders. Roof ownership and lower landing surfaces are detected with physics queries, even when the bake uses render meshes.
3. Use **Bake Roof** on Building NavMesh Baker, or **Bake All Buildings** on City NavMesh Baker.
4. Save the scene. Each building now owns a **Generated NavMesh Links** child containing bidirectional ground-to-roof links.

Links are generated automatically after every successful active building bake. The baker samples the roof's outer NavMesh edges, looks down outside the wall, and connects endpoints only when both have NavMesh positions for the chosen agent type. Lower walkable terraces can also be linked.

- **Link Spacing** controls link density (default 4 m).
- **Maximum Link Drop** limits the height difference (default 50 m).
- **Geometry Layers** must include the building colliders; **Geometry Layers** or **Blocking Layers** must include the lower landing colliders.
- **Generated Links** in the Inspector shows the resulting count.

Rebaking replaces only the generated links. **Clear Bake** removes them with the roof bake. Manually authored links are preserved. If no links appear, check the ground bake, matching agent types, colliders, layer masks and maximum drop height. There must be walkable space outside the wall for the lower endpoint.

Links connect endpoints directly; generation does not construct a collision-aware climbing trajectory. Enemies control link traversal speed themselves.

## Enemy behavior

Enemies follow NavMesh paths. On entering a NavMesh link they switch to `climbingState`, set the Animator's `Climbing` parameter, and move directly toward the link endpoint. **Jump Speed** on EnemyController controls this movement in units per second (default 4). It can be changed while traversing.

After reaching the endpoint, the enemy completes the link and resumes pursuit, or idles if its target disappeared. Attacks cannot interrupt link traversal, and group membership is retained until traversal finishes. Player climbing is unchanged; the old wall detection and CrawlWall action are not used.

The baker continues to support incremental asynchronous baking, forced rebuilds, layer filtering, nearby obstructions and optional runtime baking. City baking processes buildings sequentially. Rebake after changing geometry or building transforms.
