# Holds after carving

`EdgeSpawner` listens for completed chunk rebuilds. After each carve, it replaces hold data only for affected chunks. Chunks already loaded by `NearbyHoldChunkLoader` manifest their new holds immediately; unloaded chunks retain placement data until approached.

Edge detection uses the surviving exposed source triangles and cut faces, with one cell of neighbouring geometry for edges crossing chunk boundaries. Buried cell faces are never generated or passed to the edge detector. Newly exposed interior cube faces are included because they can form usable ledges. Empty chunks skip edge calculation and release their old holds.

The shell calculator matches overlapping collinear triangle edges, including unequal subdivisions between source triangles and caps. A bounding-volume tree finds nearby candidate segments before geometric tolerance checks, so floating-point rounding cannot separate matching edges into different line-key buckets. It accepts convex joins of at least 80 degrees, following the existing edge rules. The solver's **Calculate Only Top** setting filters using world orientation. A shared position registry prevents duplicate holds where chunks or several edges meet.

The existing hold pool, spacing, source ownership, destruction cleanup, and proximity loader remain in use. Disabling and re-enabling the spawner rebuilds from the current carved shell. This refresh concerns surviving destructible chunks, not moving debris rigidbodies.

Hold rebuilding always uses jobs; the managed detector is retained for editor regression checks only.

**Tools > Destruction > Validate Shell Edges** checks unequal edge subdivisions, near-coincident edges across rounding boundaries, rotated edge coverage, separated-edge rejection, concave/flat rejection, and top filtering under rotation. Runtime validation additionally covered a new ledge at a chunk seam, duplicate prevention, unchanged distant holds, proximity reload, and complete removal.
