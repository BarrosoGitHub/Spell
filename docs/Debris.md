# Extracted debris

Add `DebrisManager` to the object with `GridBooleanDestruction` to configure it before play. Alternatively, assign a shared manager in the destruction component's **Debris Manager** field. With **Generate Debris** enabled, a local manager is added automatically on the first hit if none is assigned.

- **Piece Count**: 1 keeps the entire extracted volume together; 2–64 partitions it. The actual count cannot exceed the number of removed cells.
- **Fracture Style**: random spatial groups or regular groups split along their longest world axis. Both use the existing baked cell faces; random grouping does not introduce arbitrary cuts through cells. Disconnected regions can share a rigidbody.
- **Outward Speed**: initial separation velocity for multiple pieces.
- **Lifetime**: cleanup after this many seconds; 0 retains debris indefinitely. Each new generation starts its own lifetime.
- **Mass Per Cell**: approximate mass; impact breakup divides the parent's mass between children by cell count.

Visual geometry uses the source surface triangles and the same baked caps as carving. All extracted cells belong to exactly one piece. Internal faces within a piece are omitted; newly exposed faces appear when that piece breaks. Accuracy matches the existing carving representation, including its legacy cap-mask approximation when Smooth Caps is disabled. Simple bounding-box colliders deliberately approximate the visual geometry.

Each spawned object has a `Debris` component. Its ground-impact defaults come from the manager and can be overridden on the piece. A collision qualifies when its layer matches **Ground Layers**, its contact normal points sufficiently against gravity, and its normal impact speed exceeds **Minimum Impact Speed**. Debris-to-debris collisions are excluded. **Break Chance**, **Pieces On Break**, **Maximum Break Depth**, and **Impact Grace Period** control subsequent breakup. A single cell cannot break further. Children inherit the parent's current pose, point velocity, angular velocity, and impact settings.

The manager owns its generated objects and meshes; destroying the manager cleans them up. Each extraction shares one geometry snapshot across its descendants. No Rigidbody pooling or global active-piece cap is applied, so retaining many pieces indefinitely increases physics and memory cost.

## Validation

Run **Tools > Destruction > Validate Debris Geometry** for geometry checks without modifying the open scene. These cover both partition modes, count clamping, volume conservation, closed boundaries, removal of internal faces, and repeated transformed fracture generations.
