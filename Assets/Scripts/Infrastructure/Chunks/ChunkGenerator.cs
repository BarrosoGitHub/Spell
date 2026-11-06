using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

public class GridBooleanDestruction : MonoBehaviour
{
    [Header("Grid")]
    [Min(.01f)] public float cellSize = .5f;
    [Min(2)] public int chunkCells = 8;

    [Header("Damage")]
    [Min(1)] public int removeSize = 3;
    public bool irregularExplosionShape = true;

    [Header("Debris")]
    public bool generateDebris = true;
    [Tooltip("Optional shared manager. A local manager is added automatically when needed.")]
    public DebrisManager debrisManager;
    readonly List<int> removedThisHit = new();

    [Header("Explosion Shape")]
    [Range(.2f, 1.5f)] public float explosionDepthScale = .55f;
    [Range(0f, 1f)] public float explosionInset = .35f;
    [Range(0f, 1f)] public float explosionEdgeNoise = .22f;
    [Range(0f, 1f)] public float explosionChipChance = .22f;
    [Range(0f, 1f)] public float explosionLobeStrength = .35f;

    [Header("Surface")]
    [Tooltip("Bake caps along source-mesh cut lines instead of a stair-stepped triangle mask.")]
    public bool smoothCaps = true;
    [Tooltip("Legacy mask resolution, used only when Smooth Caps is disabled.")]
    [Range(2, 32)] public int capSubdivisions = 12;

    const float EPS = .00001f;

    const byte CELL_SOLID = 1 << 0;
    const byte CELL_REMOVED = 1 << 1;

    const byte CAP_NONE = 0;
    const byte CAP_FULL = 1;
    const byte CAP_PARTIAL = 2;
    const byte CAP_CONTOUR = 3;

    static readonly Vector3Int[] dirs =
    {
        Vector3Int.right, Vector3Int.left,
        Vector3Int.up, Vector3Int.down,
        new(0, 0, 1), new(0, 0, -1)
    };

    static readonly Vector3 insideRayDir =
        new Vector3(1, .371f, .529f).normalized;

    MeshFilter sourceMF;
    MeshRenderer sourceMR;
    MeshCollider sourceMC;
    MeshCollider bakeCollider;
    Mesh sourceMesh;

    Vector3[] srcVerts;
    Vector3[] srcNormals;
    Vector4[] srcTangents;
    Vector2[] srcUV;
    Vector2[] srcUV2;
    int[] srcTris;

    bool hasTangents;
    bool hasUV;
    bool hasUV2;

    Vector3 gridOrigin;
    Vector3Int gridSize;
    Vector3Int chunkGridSize;

    // Compact grid state. No Cell objects and no per-cell Lists.
    Cell[] cells;

    // Only surface cells get geometry metadata.
    SurfaceCellData[] surfaceCells = Array.Empty<SurfaceCellData>();

    // Shared packed surface geometry for every surface cell.
    V[] surfaceVertices = Array.Empty<V>();
    int[] surfaceIndices = Array.Empty<int>();

    // Six compact cap descriptors per surface cell.
    CapFace[] capFaces = Array.Empty<CapFace>();

    // Partial surface caps are stored as triangle occupancy masks.
    // For subdivision 12 there are 12*12*2 = 288 possible mini-triangles,
    // so each partial cap needs only 288 bits = 36 bytes.
    byte[] capMaskBytes = Array.Empty<byte>();
    ContourCap[] contourCaps = Array.Empty<ContourCap>();
    Vector2[] contourVertices = Array.Empty<Vector2>();
    int[] contourIndices = Array.Empty<int>();

    // Runtime-only scratch for de-duplicating partial-cap grid vertices.
    // Reused for every cap rebuild, so damage creates no temporary arrays.
    int[] capVertexIndexScratch = Array.Empty<int>();
    int[] capVertexStampScratch = Array.Empty<int>();
    int capVertexStamp;

    public static event Action<GridBooleanDestruction> Generated;
    public event Action<int> CellRemoved;
    // Raised after geometry/colliders are current. IDs are valid for this callback only.
    public event Action<IReadOnlyCollection<int>> ChunksRebuilt;
    public event Action Destroying;
    public bool IsReady { get; private set; }

    // Position is in source-mesh local space. Bias into the solid to resolve
    // points exactly on a surface or grid plane to one deterministic owner.
    public bool TryGetHoldCell(Vector3 position, Vector3 outwardNormal, out int cellId)
    {
        cellId = -1;
        if (!IsReady) return false;
        Vector3 biased = position - outwardNormal.normalized * (cellSize * .001f);
        Vector3 max = gridOrigin + (Vector3)gridSize * cellSize;
        float epsilon = cellSize * .002f;
        if (biased.x < gridOrigin.x - epsilon || biased.y < gridOrigin.y - epsilon ||
            biased.z < gridOrigin.z - epsilon || biased.x > max.x + epsilon ||
            biased.y > max.y + epsilon || biased.z > max.z + epsilon) return false;
        biased = new Vector3(Mathf.Clamp(biased.x, gridOrigin.x + epsilon, max.x - epsilon),
            Mathf.Clamp(biased.y, gridOrigin.y + epsilon, max.y - epsilon),
            Mathf.Clamp(biased.z, gridOrigin.z + epsilon, max.z - epsilon));
        Vector3Int coord = PositionToCell(biased);
        if (!Valid(coord)) return false;
        int id = Index(coord);
        if (!cells[id].solid || cells[id].removed) return false;
        cellId = id;
        return true;
    }

    public int GetChunkIdForCell(int cellId)
    {
        if (cellId < 0 || cellId >= cells.Length)
            return -1;

        return ChunkId(CoordFromIndex(cellId));
    }

    public Bounds GetChunkWorldBounds(int chunkId)
    {
        if (chunks == null || chunkId < 0 || chunkId >= chunks.Length || chunks[chunkId] == null)
            return new Bounds(transform.position, Vector3.zero);

        Bounds localBounds = chunks[chunkId].bounds;
        Vector3 center = transform.TransformPoint(localBounds.center);
        Vector3 extents = localBounds.extents;
        Matrix4x4 matrix = transform.localToWorldMatrix;
        Vector3 worldExtents = new(
            Mathf.Abs(matrix.m00) * extents.x + Mathf.Abs(matrix.m01) * extents.y + Mathf.Abs(matrix.m02) * extents.z,
            Mathf.Abs(matrix.m10) * extents.x + Mathf.Abs(matrix.m11) * extents.y + Mathf.Abs(matrix.m12) * extents.z,
            Mathf.Abs(matrix.m20) * extents.x + Mathf.Abs(matrix.m21) * extents.y + Mathf.Abs(matrix.m22) * extents.z
        );
        return new Bounds(center, worldExtents * 2f);
    }

    internal bool HasChunkShell(int id) => chunks != null && id >= 0 && id < chunks.Length &&
        chunks[id] != null && chunks[id].indices.Count > 0;

    internal IEnumerable<int> RuntimeChunkIds
    {
        get { for (int i = 0; chunks != null && i < chunks.Length; i++) if (chunks[i] != null) yield return i; }
    }

    // Only exposed source/cut triangles, with one cell of context for seam adjacency.
    // No temporary Unity Mesh, collider cooking, or hidden interior faces.
    internal void BuildHoldShell(int chunkId, List<Vector3> vertices, List<int> indices)
    {
        vertices.Clear();
        indices.Clear();
        if (!HasChunkShell(chunkId)) return;
        RuntimeChunk buffer = new();
        Vector3Int start = chunks[chunkId].c * chunkCells - Vector3Int.one;
        Vector3Int end = start + Vector3Int.one * (chunkCells + 2);
        for (int z = Mathf.Max(0, start.z); z < Mathf.Min(gridSize.z, end.z); z++)
        for (int y = Mathf.Max(0, start.y); y < Mathf.Min(gridSize.y, end.y); y++)
        for (int x = Mathf.Max(0, start.x); x < Mathf.Min(gridSize.x, end.x); x++)
        {
            Vector3Int coord = new(x, y, z);
            int id = Index(coord);
            if (!cells[id].solid || cells[id].removed) continue;
            if (cells[id].surface) AddSourceSurface(buffer, id);
            AddExposedCaps(buffer, id, coord);
        }
        vertices.AddRange(buffer.vertices);
        indices.AddRange(buffer.indices);
    }

    void Start()
    {
        if (IsReady) Generated?.Invoke(this);
    }

    void OnDestroy()
    {
        Destroying?.Invoke();
    }

    RuntimeChunk[] chunks;

    // Initialization-only structures. Released after baking.
    Dictionary<int, SurfaceBuildData> surfaceBuilders;
    Dictionary<Vector3Int, bool> insideCache;
    List<byte> capMaskBuilder;
    byte[] capMaskScratch;
    byte[] capCornerScratch;
    Dictionary<int, int> capMaskHeads;
    List<int> capMaskNext;
    CapContourBaker contourBaker;
    List<ContourCap> contourCapBuilder;
    List<Vector2> contourVertexBuilder;
    List<int> contourIndexBuilder;
    Dictionary<int, int> contourHeads;
    List<int> contourNext;
    Vector3 contourOrigin, contourU, contourV, contourNormal;

    readonly HashSet<int> dirtyChunks = new();

    struct V
    {
        public Vector3 p;
        public Vector3 n;
        public Vector4 tangent;
        public Vector2 uv;
        public Vector2 uv2;
    }

    // Intentionally tiny. On normal CLR layout this is typically ~8 bytes.
    // surfaceDataIndex == -1 means this is not an original-mesh surface cell.
    struct Cell
    {
        public int surfaceDataIndex;
        public byte flags;

        public bool surface => surfaceDataIndex >= 0;

        public bool solid
        {
            get => (flags & CELL_SOLID) != 0;
            set
            {
                if (value) flags |= CELL_SOLID;
                else flags &= unchecked((byte)~CELL_SOLID);
            }
        }

        public bool removed
        {
            get => (flags & CELL_REMOVED) != 0;
            set
            {
                if (value) flags |= CELL_REMOVED;
                else flags &= unchecked((byte)~CELL_REMOVED);
            }
        }
    }

    // Temporary surface-cell builder used only while clipping source triangles.
    // Importantly, only actual surface cells allocate these Lists.
    sealed class SurfaceBuildData
    {
        public readonly List<V> vertices = new();
        public readonly List<int> triangles = new();
    }

    // Retained metadata points into the two shared surface arrays.
    struct SurfaceCellData
    {
        public int vertexStart;
        public int vertexCount;
        public int indexStart;
        public int indexCount;
    }

    // Retained cap descriptor. Six descriptors exist only for SURFACE cells.
    // Interior cells retain ZERO cap payload; AddFullCapIndexed() generates their
    // square faces directly when a neighbour is removed.
    //
    // CAP_FULL also retains no payload.
    // CAP_PARTIAL points at a fixed-size triangle mask in capMaskBytes.
    struct CapFace
    {
        // Zero-initialized descriptors are empty. Positive offsets use +2.
        int encoded;
        public byte type => encoded < 0 ? CAP_CONTOUR : encoded == 0 ? CAP_NONE : encoded == 1 ? CAP_FULL : CAP_PARTIAL;
        public int maskStart => encoded - 2;
        public int contourIndex => -encoded - 1;
        public static CapFace Full => new CapFace { encoded = 1 };
        public static CapFace Partial(int offset) => new CapFace { encoded = checked(offset + 2) };
        public static CapFace Contour(int index) => new CapFace { encoded = checked(-index - 1) };
    }

    struct ContourCap
    {
        public int vertexStart, vertexCount, indexStart, indexCount;
    }

    sealed class RuntimeChunk
    {
        public Vector3Int c;
        public Bounds bounds;

        public Mesh mesh;
        public MeshRenderer renderer;
        public MeshCollider collider;

        // Do NOT preallocate 1024/2048 elements per chunk. At 0.1 m cell size
        // there can be many chunks, and those unused capacities become expensive.
        public readonly List<Vector3> vertices = new();
        public readonly List<Vector3> normals = new();
        public readonly List<Vector4> tangents = new();
        public readonly List<Vector2> uv = new();
        public readonly List<Vector2> uv2 = new();
        public readonly List<int> indices = new();
        public readonly List<int> triangleOwners = new();

        public void Clear()
        {
            vertices.Clear();
            normals.Clear();
            tangents.Clear();
            uv.Clear();
            uv2.Clear();
            indices.Clear();
            triangleOwners.Clear();
        }

        public void TrimBuffers()
        {
            vertices.TrimExcess();
            normals.TrimExcess();
            tangents.TrimExcess();
            uv.TrimExcess();
            uv2.TrimExcess();
            indices.TrimExcess();
            triangleOwners.TrimExcess();
        }
    }

    void OnValidate()
    {
        removeSize = Mathf.Max(1, removeSize);
        if (removeSize % 2 == 0)
            removeSize++;
    }

    void Awake()
    {
        sourceMF = GetComponent<MeshFilter>();
        sourceMR = GetComponent<MeshRenderer>();
        sourceMC = GetComponent<MeshCollider>();

        if (!sourceMF || !sourceMR || !sourceMF.sharedMesh)
        {
            
            enabled = false;
            return;
        }

        // Clone only because ReadSource may need to recalculate normals.
        sourceMesh = Instantiate(sourceMF.sharedMesh);

        ReadSource();
        CreateBakeCollider();

        GenerateGrid();

        CutMeshIntoGrid();
        FinalizeSurfaceGeometry();

        bool oldBackfaces = Physics.queriesHitBackfaces;
        Physics.queriesHitBackfaces = true;

        MarkSolidCells();

        BakeSurfaceCaps();
        InitializeCapRuntimeScratch();

        Physics.queriesHitBackfaces = oldBackfaces;

        ReleaseBakeCaches();

        CreateChunks();

        BuildAllChunks();
        TrimRuntimeChunkBuffers();

        sourceMR.enabled = false;
        if (sourceMC)
            sourceMC.enabled = false;

        IsReady = true;
        ReleaseSourceBakeData();
    }

    void Update()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        Camera cam = Camera.main;
        if (!cam)
            return;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!PickCell(ray, out int cellId, out Vector3 hitNormalLocal))
        {
            return;
        }

        Cell clicked = cells[cellId];
        Vector3Int clickedCoord = CoordFromIndex(cellId);

        if (!clicked.solid || clicked.removed)
        {
            return;
        }

        dirtyChunks.Clear();

        removedThisHit.Clear();

        int removed = irregularExplosionShape
            ? RemoveIrregularExplosion(clickedCoord, hitNormalLocal)
            : RemoveCube(clickedCoord);

        if (removed == 0)
            return;

        RebuildDirtyChunks();

        if (generateDebris)
        {
            if (!debrisManager) debrisManager = GetComponent<DebrisManager>();
            if (!debrisManager) debrisManager = gameObject.AddComponent<DebrisManager>();
            debrisManager.Generate(this, removedThisHit);
        }
    }

    // =========================================================
    // CLICK DETECTION
    // =========================================================

    bool PickCell(Ray worldRay, out int cellId, out Vector3 hitNormalLocal)
    {
        Matrix4x4 worldToLocal = transform.worldToLocalMatrix;

        Vector3 origin = worldToLocal.MultiplyPoint3x4(worldRay.origin);
        Vector3 direction = worldToLocal.MultiplyVector(worldRay.direction).normalized;

        Ray ray = new(origin, direction);

        float bestDistance = float.MaxValue;
        int bestCell = -1;
        Vector3 bestNormal = Vector3.zero;

        foreach (RuntimeChunk chunk in chunks)
        {
            if (chunk == null || !chunk.renderer.enabled || chunk.indices.Count == 0)
                continue;

            if (!chunk.bounds.IntersectRay(ray))
                continue;

            for (int i = 0, tri = 0; i < chunk.indices.Count; i += 3, tri++)
            {
                Vector3 a = chunk.vertices[chunk.indices[i]];
                Vector3 b = chunk.vertices[chunk.indices[i + 1]];
                Vector3 c = chunk.vertices[chunk.indices[i + 2]];

                if (!RayTriangle(origin, direction, a, b, c, out float distance))
                    continue;

                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestCell = chunk.triangleOwners[tri];
                bestNormal = Vector3.Cross(b - a, c - a).normalized;
            }
        }

        cellId = bestCell;
        hitNormalLocal = bestNormal;
        return bestCell >= 0;
    }

    bool RayTriangle(
        Vector3 origin,
        Vector3 direction,
        Vector3 a,
        Vector3 b,
        Vector3 c,
        out float distance)
    {
        Vector3 e1 = b - a;
        Vector3 e2 = c - a;

        Vector3 p = Vector3.Cross(direction, e2);
        float det = Vector3.Dot(e1, p);

        if (Mathf.Abs(det) < EPS)
        {
            distance = 0;
            return false;
        }

        float inv = 1f / det;

        Vector3 t = origin - a;
        float u = Vector3.Dot(t, p) * inv;

        if (u < 0 || u > 1)
        {
            distance = 0;
            return false;
        }

        Vector3 q = Vector3.Cross(t, e1);
        float v = Vector3.Dot(direction, q) * inv;

        if (v < 0 || u + v > 1)
        {
            distance = 0;
            return false;
        }

        distance = Vector3.Dot(e2, q) * inv;
        return distance > EPS;
    }

    // =========================================================
    // GRID
    // =========================================================

    void GenerateGrid()
    {
        Bounds b = sourceMesh.bounds;
        gridOrigin = b.min;

        gridSize = new Vector3Int(
            Mathf.CeilToInt(b.size.x / cellSize),
            Mathf.CeilToInt(b.size.y / cellSize),
            Mathf.CeilToInt(b.size.z / cellSize)
        );

        cells = new Cell[gridSize.x * gridSize.y * gridSize.z];

        // Arrays of structs come zeroed. We only need -1 for surfaceDataIndex.
        for (int i = 0; i < cells.Length; i++)
            cells[i].surfaceDataIndex = -1;
    }

    int Index(Vector3Int c) =>
        c.x + gridSize.x * (c.y + gridSize.y * c.z);

    Vector3Int CoordFromIndex(int index)
    {
        int xy = gridSize.x * gridSize.y;
        int z = index / xy;
        int rem = index - z * xy;
        int y = rem / gridSize.x;
        int x = rem - y * gridSize.x;
        return new Vector3Int(x, y, z);
    }

    bool Valid(Vector3Int c) =>
        c.x >= 0 && c.y >= 0 && c.z >= 0 &&
        c.x < gridSize.x &&
        c.y < gridSize.y &&
        c.z < gridSize.z;

    Vector3Int PositionToCell(Vector3 p)
    {
        p -= gridOrigin;

        return new Vector3Int(
            Mathf.FloorToInt(p.x / cellSize),
            Mathf.FloorToInt(p.y / cellSize),
            Mathf.FloorToInt(p.z / cellSize)
        );
    }

    Vector3 CellCenter(Vector3Int c) =>
        gridOrigin + ((Vector3)c + Vector3.one * .5f) * cellSize;

    Bounds CellBounds(Vector3Int c)
    {
        Vector3 min = gridOrigin + (Vector3)c * cellSize;

        return new Bounds(
            min + Vector3.one * cellSize * .5f,
            Vector3.one * cellSize
        );
    }

    // =========================================================
    // SOURCE
    // =========================================================

    void ReadSource()
    {
        srcVerts = sourceMesh.vertices;
        srcNormals = sourceMesh.normals;

        if (srcNormals.Length != srcVerts.Length)
        {
            sourceMesh.RecalculateNormals();
            srcNormals = sourceMesh.normals;
        }

        srcTangents = sourceMesh.tangents;
        srcUV = sourceMesh.uv;
        srcUV2 = sourceMesh.uv2;
        srcTris = sourceMesh.triangles;

        hasTangents = srcTangents.Length == srcVerts.Length;
        hasUV = srcUV.Length == srcVerts.Length;
        hasUV2 = srcUV2.Length == srcVerts.Length;
    }

    void CreateBakeCollider()
    {
        GameObject go = new("__DestructionBakeCollider");
        go.transform.SetParent(transform, false);
        go.layer = 2;
        go.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;

        bakeCollider = go.AddComponent<MeshCollider>();
        bakeCollider.sharedMesh = sourceMesh;
    }

    V SourceVertex(int i) => new()
    {
        p = srcVerts[i],
        n = srcNormals[i],
        tangent = hasTangents ? srcTangents[i] : Vector4.zero,
        uv = hasUV ? srcUV[i] : Vector2.zero,
        uv2 = hasUV2 ? srcUV2[i] : Vector2.zero
    };

    // =========================================================
    // SURFACE CUT + PACKING
    // =========================================================

    void CutMeshIntoGrid()
    {
        surfaceBuilders = new Dictionary<int, SurfaceBuildData>();

        for (int i = 0; i < srcTris.Length; i += 3)
        {
            V a = SourceVertex(srcTris[i]);
            V b = SourceVertex(srcTris[i + 1]);
            V c = SourceVertex(srcTris[i + 2]);

            Vector3 min = Vector3.Min(a.p, Vector3.Min(b.p, c.p));
            Vector3 max = Vector3.Max(a.p, Vector3.Max(b.p, c.p));

            Vector3Int from = PositionToCell(min);
            Vector3Int to = PositionToCell(max);

            Clamp(ref from);
            Clamp(ref to);

            for (int z = from.z; z <= to.z; z++)
            for (int y = from.y; y <= to.y; y++)
            for (int x = from.x; x <= to.x; x++)
                ClipTriangle(a, b, c, new Vector3Int(x, y, z));
        }
    }

    void Clamp(ref Vector3Int c)
    {
        c.x = Mathf.Clamp(c.x, 0, gridSize.x - 1);
        c.y = Mathf.Clamp(c.y, 0, gridSize.y - 1);
        c.z = Mathf.Clamp(c.z, 0, gridSize.z - 1);
    }

    void ClipTriangle(V a, V b, V c, Vector3Int coord)
    {
        Bounds bnd = CellBounds(coord);

        // These lists are still temporary allocations. They no longer survive
        // initialization, so they affect peak/GC pressure rather than retained RAM.
        List<V> input = new(8) { a, b, c };
        List<V> output = new(8);

        Clip(ref input, ref output, 0, bnd.min.x, true);
        Clip(ref input, ref output, 0, bnd.max.x, false);
        Clip(ref input, ref output, 1, bnd.min.y, true);
        Clip(ref input, ref output, 1, bnd.max.y, false);
        Clip(ref input, ref output, 2, bnd.min.z, true);
        Clip(ref input, ref output, 2, bnd.max.z, false);

        if (input.Count < 3)
            return;

        int cellId = Index(coord);

        if (!surfaceBuilders.TryGetValue(cellId, out SurfaceBuildData build))
        {
            build = new SurfaceBuildData();
            surfaceBuilders.Add(cellId, build);
        }

        int start = build.vertices.Count;
        build.vertices.AddRange(input);

        for (int i = 1; i < input.Count - 1; i++)
        {
            build.triangles.Add(start);
            build.triangles.Add(start + i);
            build.triangles.Add(start + i + 1);
        }
    }

    void FinalizeSurfaceGeometry()
    {
        int surfaceCount = surfaceBuilders.Count;
        surfaceCells = new SurfaceCellData[surfaceCount];

        long totalVerticesLong = 0;
        long totalIndicesLong = 0;

        foreach (SurfaceBuildData build in surfaceBuilders.Values)
        {
            totalVerticesLong += build.vertices.Count;
            totalIndicesLong += build.triangles.Count;
        }

        if (totalVerticesLong > int.MaxValue || totalIndicesLong > int.MaxValue)
            throw new InvalidOperationException("Stored surface geometryetry exceeds a single managed array.");

        surfaceVertices = new V[(int)totalVerticesLong];
        surfaceIndices = new int[(int)totalIndicesLong];

        int surfaceIndex = 0;
        int vertexCursor = 0;
        int indexCursor = 0;

        foreach (KeyValuePair<int, SurfaceBuildData> pair in surfaceBuilders)
        {
            int cellId = pair.Key;
            SurfaceBuildData build = pair.Value;

            Cell cell = cells[cellId];
            cell.surfaceDataIndex = surfaceIndex;
            cells[cellId] = cell;

            SurfaceCellData data = new()
            {
                vertexStart = vertexCursor,
                vertexCount = build.vertices.Count,
                indexStart = indexCursor,
                indexCount = build.triangles.Count
            };

            for (int i = 0; i < build.vertices.Count; i++)
                surfaceVertices[vertexCursor + i] = build.vertices[i];

            for (int i = 0; i < build.triangles.Count; i++)
                surfaceIndices[indexCursor + i] = build.triangles[i];

            surfaceCells[surfaceIndex] = data;

            vertexCursor += build.vertices.Count;
            indexCursor += build.triangles.Count;
            surfaceIndex++;
        }

        // Drop every per-surface-cell List. From here onward only packed arrays remain.
        surfaceBuilders.Clear();
        surfaceBuilders = null;
    }

    void Clip(
        ref List<V> input,
        ref List<V> output,
        int axis,
        float plane,
        bool greater)
    {
        output.Clear();

        if (input.Count == 0)
            return;

        V previous = input[^1];
        bool previousInside = PlaneInside(previous.p, axis, plane, greater);

        foreach (V current in input)
        {
            bool inside = PlaneInside(current.p, axis, plane, greater);

            if (previousInside && inside)
            {
                output.Add(current);
            }
            else if (previousInside != inside)
            {
                output.Add(Intersect(previous, current, axis, plane));
                if (inside)
                    output.Add(current);
            }

            previous = current;
            previousInside = inside;
        }

        (input, output) = (output, input);
    }

    bool PlaneInside(Vector3 p, int axis, float plane, bool greater)
    {
        float v = Axis(p, axis);
        return greater ? v >= plane - EPS : v <= plane + EPS;
    }

    V Intersect(V a, V b, int axis, float plane)
    {
        float delta = Axis(b.p, axis) - Axis(a.p, axis);

        if (Mathf.Abs(delta) < EPS)
            return a;

        float t = (plane - Axis(a.p, axis)) / delta;

        return new V
        {
            p = Vector3.LerpUnclamped(a.p, b.p, t),
            n = Vector3.Lerp(a.n, b.n, t).normalized,
            tangent = Vector4.Lerp(a.tangent, b.tangent, t),
            uv = Vector2.LerpUnclamped(a.uv, b.uv, t),
            uv2 = Vector2.LerpUnclamped(a.uv2, b.uv2, t)
        };
    }

    float Axis(Vector3 p, int axis) =>
        axis == 0 ? p.x :
        axis == 1 ? p.y :
        p.z;

    // =========================================================
    // SOLID CELLS
    // =========================================================

    void MarkSolidCells()
    {
        // Cell centers are visited once; caching them only increases peak RAM.
        insideCache = null;

        for (int i = 0; i < cells.Length; i++)
        {
            Cell cell = cells[i];

            if (cell.surface)
            {
                cell.solid = true;
            }
            else
            {
                Vector3Int coord = CoordFromIndex(i);
                cell.solid = PointInsideMesh(CellCenter(coord));
            }

            cells[i] = cell;
        }
    }

    bool PointInsideMesh(Vector3 localPoint)
    {
        Vector3Int key = new(
            Mathf.RoundToInt(localPoint.x * 100000),
            Mathf.RoundToInt(localPoint.y * 100000),
            Mathf.RoundToInt(localPoint.z * 100000)
        );

        if (insideCache != null && insideCache.TryGetValue(key, out bool cached))
            return cached;

        Vector3 origin = transform.TransformPoint(localPoint);
        Vector3 direction = transform.TransformVector(insideRayDir).normalized;

        float offset = Mathf.Max(.0001f, cellSize * .0002f);
        int hits = 0;

        for (int i = 0; i < 64; i++)
        {
            if (!bakeCollider.Raycast(new Ray(origin, direction), out RaycastHit hit, 10000))
                break;

            hits++;
            origin = hit.point + direction * offset;
        }

        bool inside = (hits & 1) == 1;

        if (insideCache != null)
            insideCache[key] = inside;

        return inside;
    }

    // =========================================================
    // CAP BAKE + TRIANGLE MASK PACKING
    // =========================================================

    void BakeSurfaceCaps()
    {
        capFaces = new CapFace[surfaceCells.Length * 6];
        if (smoothCaps)
        {
            BakeContourCaps();
            return;
        }

        int div = Mathf.Max(2, capSubdivisions);
        int trianglesPerCap = div * div * 2;
        int bytesPerCap = (trianglesPerCap + 7) >> 3;

        capMaskBuilder = new List<byte>();
        capMaskScratch = new byte[bytesPerCap];
        capCornerScratch = new byte[(div + 1) * (div + 1)];
        insideCache = new Dictionary<Vector3Int, bool>();
        capMaskHeads = new Dictionary<int, int>();
        capMaskNext = new List<int>();

        for (int cellId = 0; cellId < cells.Length; cellId++)
        {
            Cell cell = cells[cellId];
            if (!cell.surface)
                continue;

            Vector3Int coord = CoordFromIndex(cellId);
            int surfaceIndex = cell.surfaceDataIndex;

            for (int d = 0; d < 6; d++)
            {
                Vector3Int nc = coord + dirs[d];

                if (!Valid(nc))
                    continue;

                // We only need a potential cap between two solid cells.
                // If this neighbour is later removed, this face becomes exposed.
                if (!cells[Index(nc)].solid)
                    continue;

                capFaces[surfaceIndex * 6 + d] = BakeCapMask(coord, dirs[d], div, bytesPerCap);
            }
        }

        capMaskBytes = capMaskBuilder.ToArray();
        capMaskBuilder = null;
        capMaskScratch = null;
        capCornerScratch = null;
        insideCache = null;
        capMaskHeads = null;
        capMaskNext = null;
    }

    void BakeContourCaps()
    {
        contourBaker = new CapContourBaker();
        contourCapBuilder = new List<ContourCap>();
        contourVertexBuilder = new List<Vector2>();
        contourIndexBuilder = new List<int>();
        contourHeads = new Dictionary<int, int>();
        contourNext = new List<int>();
        insideCache = null;
        Func<Vector2, bool> inside = ContourInside;

        for (int cellId = 0; cellId < cells.Length; cellId++)
        {
            Cell cell = cells[cellId];
            if (!cell.surface) continue;
            Vector3Int coord = CoordFromIndex(cellId);
            for (int d = 0; d < 6; d++)
            {
                Vector3Int neighbor = coord + dirs[d];
                if (!Valid(neighbor) || !cells[Index(neighbor)].solid) continue;
                FaceBasis(coord, dirs[d], out contourOrigin, out contourU, out contourV, out contourNormal);
                contourBaker.Clear();
                AddContourSurface(cell.surfaceDataIndex);
                // Include both sides so an exactly coplanar source triangle is
                // available regardless of which cell received it while clipping.
                if (cells[Index(neighbor)].surface)
                    AddContourSurface(cells[Index(neighbor)].surfaceDataIndex);
                contourBaker.Bake(inside);
                capFaces[cell.surfaceDataIndex * 6 + d] = contourBaker.IsFull
                    ? CapFace.Full
                    : contourBaker.indices.Count == 0 ? default : CapFace.Contour(InternContour());
            }
        }
        contourCaps = contourCapBuilder.ToArray();
        contourVertices = contourVertexBuilder.ToArray();
        contourIndices = contourIndexBuilder.ToArray();
        contourBaker = null;
        contourCapBuilder = null;
        contourVertexBuilder = null;
        contourIndexBuilder = null;
        contourHeads = null;
        contourNext = null;
    }

    bool ContourInside(Vector2 point) => CapInside(
        contourOrigin + contourU * (point.x * cellSize) + contourV * (point.y * cellSize), contourNormal);

    void AddContourSurface(int surfaceIndex)
    {
        SurfaceCellData surface = surfaceCells[surfaceIndex];
        for (int i = 0; i < surface.indexCount; i += 3)
        {
            int offset = surface.indexStart + i;
            contourBaker.AddTriangle(
                surfaceVertices[surface.vertexStart + surfaceIndices[offset]].p,
                surfaceVertices[surface.vertexStart + surfaceIndices[offset + 1]].p,
                surfaceVertices[surface.vertexStart + surfaceIndices[offset + 2]].p,
                contourOrigin, contourU, contourV, contourNormal, cellSize);
        }
    }

    int InternContour()
    {
        int hash = 17;
        foreach (Vector2 vertex in contourBaker.vertices) hash = unchecked(hash * 31 + vertex.GetHashCode());
        foreach (int index in contourBaker.indices) hash = unchecked(hash * 31 + index);
        int head = contourHeads.TryGetValue(hash, out int existing) ? existing : -1;
        for (int index = head; index >= 0; index = contourNext[index])
        {
            ContourCap cap = contourCapBuilder[index];
            if (cap.vertexCount != contourBaker.vertices.Count || cap.indexCount != contourBaker.indices.Count) continue;
            bool same = true;
            for (int i = 0; same && i < cap.vertexCount; i++)
                same = contourVertexBuilder[cap.vertexStart + i].Equals(contourBaker.vertices[i]);
            for (int i = 0; same && i < cap.indexCount; i++)
                same = contourIndexBuilder[cap.indexStart + i] == contourBaker.indices[i];
            if (same) return index;
        }
        int result = contourCapBuilder.Count;
        contourCapBuilder.Add(new ContourCap {
            vertexStart = contourVertexBuilder.Count, vertexCount = contourBaker.vertices.Count,
            indexStart = contourIndexBuilder.Count, indexCount = contourBaker.indices.Count
        });
        contourVertexBuilder.AddRange(contourBaker.vertices);
        contourIndexBuilder.AddRange(contourBaker.indices);
        contourHeads[hash] = result;
        contourNext.Add(head);
        return result;
    }

    CapFace BakeCapMask(
        Vector3Int cell,
        Vector3Int dir,
        int div,
        int bytesPerCap)
    {
        FaceBasis(cell, dir, out Vector3 o, out Vector3 u, out Vector3 v, out Vector3 n);

        Array.Clear(capMaskScratch, 0, capMaskScratch.Length);
        Array.Clear(capCornerScratch, 0, capCornerScratch.Length);
        // Keep quantized sample reuse within a face without retaining every
        // sampled position in the entire mesh. Capacity is reused for all faces.
        insideCache.Clear();

        float step = cellSize / div;
        bool any = false;
        bool full = true;
        int triangleIndex = 0;

        for (int y = 0; y < div; y++)
        for (int x = 0; x < div; x++)
        {
            Vector3 a = o + u * (x * step) + v * (y * step);
            Vector3 b = o + u * ((x + 1) * step) + v * (y * step);
            Vector3 c = o + u * ((x + 1) * step) + v * ((y + 1) * step);
            Vector3 d = o + u * (x * step) + v * ((y + 1) * step);

            bool tri0Full;
            int row = y * (div + 1) + x;
            bool ia = SampleCapCorner(row, a, n);
            bool ib = SampleCapCorner(row + 1, b, n);
            bool ic = SampleCapCorner(row + div + 2, c, n);
            bool tri0Filled = SampleCapTriangle(a, b, c, n, ia, ib, ic, out tri0Full);

            if (tri0Filled)
            {
                SetMaskBit(capMaskScratch, triangleIndex);
                any = true;
            }

            if (!tri0Full)
                full = false;

            triangleIndex++;

            bool tri1Full;
            bool id = SampleCapCorner(row + div + 1, d, n);
            bool tri1Filled = SampleCapTriangle(a, c, d, n, ia, ic, id, out tri1Full);

            if (tri1Filled)
            {
                SetMaskBit(capMaskScratch, triangleIndex);
                any = true;
            }

            if (!tri1Full)
                full = false;

            triangleIndex++;
        }

        if (!any)
            return default;

        // A completely filled surface-cell face is equivalent to an interior
        // face: retain no mask and generate one simple square at runtime.
        if (full)
        {
            return CapFace.Full;
        }

        return CapFace.Partial(InternCapMask(bytesPerCap));
    }

    int InternCapMask(int bytesPerCap)
    {
        int hash = 17;
        for (int i = 0; i < bytesPerCap; i++)
            hash = unchecked(hash * 31 + capMaskScratch[i]);

        int head = capMaskHeads.TryGetValue(hash, out int existing) ? existing : -1;
        for (int index = head; index >= 0; index = capMaskNext[index])
        {
            int offset = index * bytesPerCap;
            bool equal = true;
            for (int i = 0; i < bytesPerCap; i++)
            {
                if (capMaskBuilder[offset + i] == capMaskScratch[i]) continue;
                equal = false;
                break;
            }
            // Compare bytes too: a hash collision must never change geometry.
            if (equal) return offset;
        }

        int start = capMaskBuilder.Count;
        capMaskHeads[hash] = capMaskNext.Count;
        capMaskNext.Add(head);
        for (int i = 0; i < bytesPerCap; i++)
            capMaskBuilder.Add(capMaskScratch[i]);
        return start;
    }

    bool SampleCapCorner(int index, Vector3 position, Vector3 normal)
    {
        byte value = capCornerScratch[index];
        if (value == 0)
        {
            value = CapInside(position, normal) ? (byte)2 : (byte)1;
            capCornerScratch[index] = value;
        }
        return value == 2;
    }

    // Converts the expensive geometric cap result into one bit.
    //
    // We deliberately do NOT keep exact boundary vertices anymore.
    // A mini-triangle is retained when at least half of its samples indicate
    // material. This rasterizes the cap boundary onto the subdivision grid.
    //
    // At subdivision 12 on a 0.1 m cell, the grid spacing is ~8.3 mm.
    bool SampleCapTriangle(
        Vector3 a,
        Vector3 b,
        Vector3 c,
        Vector3 n,
        bool ia,
        bool ib,
        bool ic,
        out bool completelyInside)
    {
        int cornerCount = (ia ? 1 : 0) + (ib ? 1 : 0) + (ic ? 1 : 0);

        completelyInside = cornerCount == 3;

        if (completelyInside)
            return true;

        // Two corners inside means most of the mini-triangle is normally
        // inside, so keep it without another ray test.
        if (cornerCount >= 2)
            return true;

        // For 0 or 1 inside corners, sample the centroid. This preserves
        // thin/diagonal boundary regions better than corner-only voting.
        Vector3 center = (a + b + c) / 3f;
        return CapInside(center, n);
    }

    void SetMaskBit(byte[] mask, int triangleIndex)
    {
        int byteIndex = triangleIndex >> 3;
        int bitIndex = triangleIndex & 7;
        mask[byteIndex] |= (byte)(1 << bitIndex);
    }

    bool GetMaskBit(int maskStart, int triangleIndex)
    {
        int byteIndex = maskStart + (triangleIndex >> 3);
        int bitIndex = triangleIndex & 7;

        return (capMaskBytes[byteIndex] & (1 << bitIndex)) != 0;
    }

    bool CapInside(Vector3 p, Vector3 n)
    {
        float offset = Mathf.Min(cellSize * .001f, .00025f);
        return PointInsideMesh(p - n * offset);
    }

    void ReleaseBakeCaches()
    {
        if (insideCache != null)
        {
            insideCache.Clear();
            insideCache = null;
        }

        surfaceBuilders = null;
        capMaskBuilder = null;
        capMaskScratch = null;
        capCornerScratch = null;
        capMaskHeads = null;
        capMaskNext = null;
    }

    void ReleaseSourceBakeData()
    {
        srcVerts = null;
        srcNormals = null;
        srcTangents = null;
        srcUV = null;
        srcUV2 = null;
        srcTris = null;

        if (bakeCollider)
        {
            bakeCollider.sharedMesh = null;
            Destroy(bakeCollider.gameObject);
            bakeCollider = null;
        }

        if (sourceMesh)
        {
            Destroy(sourceMesh);
            sourceMesh = null;
        }
    }

    // =========================================================
    // CHUNKS
    // =========================================================

    void CreateChunks()
    {
        chunkCells = Mathf.Max(2, chunkCells);

        chunkGridSize = new Vector3Int(
            Mathf.CeilToInt((float)gridSize.x / chunkCells),
            Mathf.CeilToInt((float)gridSize.y / chunkCells),
            Mathf.CeilToInt((float)gridSize.z / chunkCells)
        );

        chunks = new RuntimeChunk[chunkGridSize.x * chunkGridSize.y * chunkGridSize.z];
        bool[] needed = new bool[chunks.Length];

        for (int i = 0; i < cells.Length; i++)
        {
            if (!cells[i].solid)
                continue;

            Vector3Int coord = CoordFromIndex(i);
            needed[ChunkId(coord)] = true;
        }

        GameObject root = new("__RuntimeChunks");
        root.layer = gameObject.layer;
        root.transform.SetParent(transform, false);

        for (int z = 0; z < chunkGridSize.z; z++)
        for (int y = 0; y < chunkGridSize.y; y++)
        for (int x = 0; x < chunkGridSize.x; x++)
        {
            Vector3Int c = new(x, y, z);
            int id = ChunkIndex(c);

            if (!needed[id])
                continue;

            GameObject go = new($"Chunk_{x}_{y}_{z}");
            go.layer = gameObject.layer;
            go.transform.SetParent(root.transform, false);

            Mesh mesh = new() { name = $"Chunk_{x}_{y}_{z}" };
            mesh.MarkDynamic();

            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            MeshCollider mc = go.AddComponent<MeshCollider>();

            mf.sharedMesh = mesh;

            mr.sharedMaterials = sourceMR.sharedMaterials;
            mr.shadowCastingMode = sourceMR.shadowCastingMode;
            mr.receiveShadows = sourceMR.receiveShadows;

            chunks[id] = new RuntimeChunk
            {
                c = c,
                mesh = mesh,
                renderer = mr,
                collider = mc,
                bounds = ChunkBounds(c)
            };
        }
    }

    int ChunkIndex(Vector3Int c) =>
        c.x + chunkGridSize.x * (c.y + chunkGridSize.y * c.z);

    int ChunkId(Vector3Int cell) =>
        ChunkIndex(new Vector3Int(
            cell.x / chunkCells,
            cell.y / chunkCells,
            cell.z / chunkCells
        ));

    Bounds ChunkBounds(Vector3Int chunk)
    {
        Vector3Int start = new(
            chunk.x * chunkCells,
            chunk.y * chunkCells,
            chunk.z * chunkCells
        );

        Vector3Int end = new(
            Mathf.Min(start.x + chunkCells, gridSize.x),
            Mathf.Min(start.y + chunkCells, gridSize.y),
            Mathf.Min(start.z + chunkCells, gridSize.z)
        );

        Vector3 min = gridOrigin + (Vector3)start * cellSize;
        Vector3 max = gridOrigin + (Vector3)end * cellSize;

        return new Bounds((min + max) * .5f, max - min);
    }

    void BuildAllChunks()
    {
        foreach (RuntimeChunk chunk in chunks)
        {
            if (chunk != null)
                BuildChunk(chunk);
        }

        Physics.SyncTransforms();
    }

    void TrimRuntimeChunkBuffers()
    {
        if (chunks == null)
            return;

        foreach (RuntimeChunk chunk in chunks)
        {
            if (chunk != null)
                chunk.TrimBuffers();
        }
    }

    void RebuildDirtyChunks()
    {
        foreach (int id in dirtyChunks)
        {
            RuntimeChunk chunk = chunks[id];
            if (chunk != null)
            {
                chunk.collider.enabled = false;
                chunk.collider.sharedMesh = null;
            }
        }

        foreach (int id in dirtyChunks)
        {
            RuntimeChunk chunk = chunks[id];
            if (chunk != null)
                BuildChunkMesh(chunk);
        }

        foreach (int id in dirtyChunks)
        {
            RuntimeChunk chunk = chunks[id];

            if (chunk == null || chunk.indices.Count == 0)
                continue;

            // Assigning a changed mesh to MeshCollider is where Unity may cook
            // the updated collision representation.
            chunk.collider.sharedMesh = chunk.mesh;
            chunk.collider.enabled = true;
        }

        Physics.SyncTransforms();

        ChunksRebuilt?.Invoke(dirtyChunks);
        dirtyChunks.Clear();
    }

    void BuildChunk(RuntimeChunk chunk)
    {
        chunk.collider.enabled = false;
        chunk.collider.sharedMesh = null;

        BuildChunkMesh(chunk);

        if (chunk.indices.Count > 0)
        {
            chunk.collider.sharedMesh = chunk.mesh;
            chunk.collider.enabled = true;
        }
    }

    void BuildChunkMesh(
        RuntimeChunk chunk)
    {
        chunk.Clear();

        Vector3Int start = new(
            chunk.c.x * chunkCells,
            chunk.c.y * chunkCells,
            chunk.c.z * chunkCells
        );

        Vector3Int end = new(
            Mathf.Min(start.x + chunkCells, gridSize.x),
            Mathf.Min(start.y + chunkCells, gridSize.y),
            Mathf.Min(start.z + chunkCells, gridSize.z)
        );

        for (int z = start.z; z < end.z; z++)
        for (int y = start.y; y < end.y; y++)
        for (int x = start.x; x < end.x; x++)
        {
            Vector3Int coord = new(x, y, z);
            int cellId = Index(coord);
            Cell cell = cells[cellId];

            if (cell.removed || !cell.solid)
                continue;

            if (cell.surface)
            {
                AddSourceSurface(chunk, cellId);
            }

            AddExposedCaps(chunk, cellId, coord);
        }

        Mesh mesh = chunk.mesh;
        mesh.Clear(false);

        if (chunk.indices.Count == 0)
        {
            chunk.renderer.enabled = false;

            return;
        }

        mesh.indexFormat =
            chunk.vertices.Count > 65535
                ? IndexFormat.UInt32
                : IndexFormat.UInt16;

        mesh.SetVertices(chunk.vertices);
        mesh.SetNormals(chunk.normals);

        if (hasTangents)
            mesh.SetTangents(chunk.tangents);
        if (hasUV)
            mesh.SetUVs(0, chunk.uv);
        if (hasUV2)
            mesh.SetUVs(1, chunk.uv2);

        mesh.SetTriangles(chunk.indices, 0, false);
        mesh.bounds = chunk.bounds;

        chunk.renderer.enabled = true;
    }

    // =========================================================
    // TRIANGLE OWNERSHIP / PACKED SURFACE DATA
    // =========================================================

    void AddSourceSurface(
        RuntimeChunk chunk,
        int cellId)
    {
        Cell cell = cells[cellId];
        SurfaceCellData data = surfaceCells[cell.surfaceDataIndex];

        int start = chunk.vertices.Count;

        int vertexEnd = data.vertexStart + data.vertexCount;
        for (int i = data.vertexStart; i < vertexEnd; i++)
        {
            V v = surfaceVertices[i];

            chunk.vertices.Add(v.p);
            chunk.normals.Add(v.n);

            if (hasTangents)
                chunk.tangents.Add(v.tangent);
            if (hasUV)
                chunk.uv.Add(v.uv);
            if (hasUV2)
                chunk.uv2.Add(v.uv2);
        }

        int indexEnd = data.indexStart + data.indexCount;
        for (int i = data.indexStart; i < indexEnd; i += 3)
        {
            chunk.indices.Add(start + surfaceIndices[i]);
            chunk.indices.Add(start + surfaceIndices[i + 1]);
            chunk.indices.Add(start + surfaceIndices[i + 2]);

            chunk.triangleOwners.Add(cellId);
        }
    }

    void AddExposedCaps(
        RuntimeChunk chunk,
        int cellId,
        Vector3Int coord)
    {
        Cell cell = cells[cellId];

        for (int d = 0; d < 6; d++)
        {
            Vector3Int nc = coord + dirs[d];

            if (!Valid(nc))
                continue;

            Cell neighbor = cells[Index(nc)];

            if (!neighbor.removed)
                continue;

            if (cell.surface)
            {
                AddCachedCap(chunk, cellId, coord, d);
            }
            else
            {
                AddFullCapIndexed(chunk, cellId, coord, dirs[d]);
            }
        }
    }

    void AddCachedCap(
        RuntimeChunk chunk,
        int cellId,
        Vector3Int coord,
        int d)
    {
        Cell cell = cells[cellId];
        int surfaceIndex = cell.surfaceDataIndex;

        if (surfaceIndex < 0)
            return;

        CapFace cap = capFaces[surfaceIndex * 6 + d];

        if (cap.type == CAP_NONE)
        {
            return;
        }

        Vector3Int dir = dirs[d];

        if (cap.type == CAP_FULL)
        {
            AddFullCapIndexed(chunk, cellId, coord, dir);
            return;
        }

        if (cap.type == CAP_CONTOUR)
        {
            AddContourCap(chunk, cellId, coord, dir, cap.contourIndex);
            return;
        }

        FaceBasis(coord, dir, out Vector3 o, out Vector3 u, out Vector3 v, out Vector3 n);

        Vector3 td = dir.x != 0 ? Vector3.up : Vector3.right;
        Vector4 tangent = new(td.x, td.y, td.z, 1);

        int div = Mathf.Max(2, capSubdivisions);
        int gridWidth = div + 1;
        float step = cellSize / div;
        Vector3 stepU = u * step;
        Vector3 stepV = v * step;

        int stamp = BeginCapVertexStamp();
        bool flipWinding = Vector3.Dot(Vector3.Cross(u, v), n) < 0f;
        int triangleIndex = 0;

        // Same mask and same triangles as before, but neighboring triangles now
        // share grid vertices. At subdivision 12 a partial cap has at most
        // 13x13 = 169 unique vertices instead of up to 864 soup entries.
        for (int y = 0; y < div; y++)
        {
            int row0 = y * gridWidth;
            int row1 = (y + 1) * gridWidth;

            for (int x = 0; x < div; x++)
            {
                int aGrid = row0 + x;
                int bGrid = row0 + x + 1;
                int cGrid = row1 + x + 1;
                int dGrid = row1 + x;

                if (GetMaskBit(cap.maskStart, triangleIndex))
                {
                    int ia = GetOrAddCapGridVertex(chunk, aGrid, x, y, stamp, o, stepU, stepV, n, tangent);
                    int ib = GetOrAddCapGridVertex(chunk, bGrid, x + 1, y, stamp, o, stepU, stepV, n, tangent);
                    int ic = GetOrAddCapGridVertex(chunk, cGrid, x + 1, y + 1, stamp, o, stepU, stepV, n, tangent);

                    AddIndexedCapTriangle(chunk, ia, ib, ic, flipWinding, cellId);
                }

                triangleIndex++;

                if (GetMaskBit(cap.maskStart, triangleIndex))
                {
                    int ia = GetOrAddCapGridVertex(chunk, aGrid, x, y, stamp, o, stepU, stepV, n, tangent);
                    int ic = GetOrAddCapGridVertex(chunk, cGrid, x + 1, y + 1, stamp, o, stepU, stepV, n, tangent);
                    int id = GetOrAddCapGridVertex(chunk, dGrid, x, y + 1, stamp, o, stepU, stepV, n, tangent);

                    AddIndexedCapTriangle(chunk, ia, ic, id, flipWinding, cellId);
                }

                triangleIndex++;
            }
        }
    }

    void InitializeCapRuntimeScratch()
    {
        if (smoothCaps) return;
        int div = Mathf.Max(2, capSubdivisions);
        int required = (div + 1) * (div + 1);

        capVertexIndexScratch = new int[required];
        capVertexStampScratch = new int[required];
        capVertexStamp = 0;
    }

    void AddContourCap(RuntimeChunk chunk, int cellId, Vector3Int coord, Vector3Int dir,
        int contourIndex)
    {
        ContourCap cap = contourCaps[contourIndex];
        FaceBasis(coord, dir, out Vector3 origin, out Vector3 u, out Vector3 v, out Vector3 normal);
        Vector3 td = dir.x != 0 ? Vector3.up : Vector3.right;
        Vector4 tangent = new(td.x, td.y, td.z, 1);
        bool flipWinding = Vector3.Dot(Vector3.Cross(u, v), normal) < 0;
        int firstVertex = chunk.vertices.Count;
        for (int i = 0; i < cap.vertexCount; i++)
        {
            Vector2 point = contourVertices[cap.vertexStart + i];
            AddCapVertex(chunk, origin + u * (point.x * cellSize) + v * (point.y * cellSize), normal, tangent);
        }
        for (int i = 0; i < cap.indexCount; i += 3)
        {
            int index = cap.indexStart + i;
            AddIndexedCapTriangle(chunk, firstVertex + contourIndices[index],
                firstVertex + contourIndices[index + 1], firstVertex + contourIndices[index + 2], flipWinding, cellId);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int BeginCapVertexStamp()
    {
        if (capVertexStamp == int.MaxValue)
        {
            Array.Clear(capVertexStampScratch, 0, capVertexStampScratch.Length);
            capVertexStamp = 1;
            return capVertexStamp;
        }

        capVertexStamp++;
        if (capVertexStamp == 0)
            capVertexStamp = 1;

        return capVertexStamp;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int GetOrAddCapGridVertex(
        RuntimeChunk chunk,
        int gridId,
        int gx,
        int gy,
        int stamp,
        Vector3 origin,
        Vector3 stepU,
        Vector3 stepV,
        Vector3 normal,
        Vector4 tangent)
    {
        if (capVertexStampScratch[gridId] == stamp)
            return capVertexIndexScratch[gridId];

        int index = chunk.vertices.Count;
        Vector3 position = origin + stepU * gx + stepV * gy;

        AddCapVertex(chunk, position, normal, tangent);

        capVertexStampScratch[gridId] = stamp;
        capVertexIndexScratch[gridId] = index;
        return index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void AddIndexedCapTriangle(
        RuntimeChunk chunk,
        int a,
        int b,
        int c,
        bool flipWinding,
        int owner)
    {
        if (flipWinding)
        {
            chunk.indices.Add(a);
            chunk.indices.Add(c);
            chunk.indices.Add(b);
        }
        else
        {
            chunk.indices.Add(a);
            chunk.indices.Add(b);
            chunk.indices.Add(c);
        }

        chunk.triangleOwners.Add(owner);
    }

    void AddFullCapIndexed(
        RuntimeChunk chunk,
        int cellId,
        Vector3Int cell,
        Vector3Int dir)
    {
        FaceBasis(cell, dir, out Vector3 o, out Vector3 u, out Vector3 v, out Vector3 n);

        u *= cellSize;
        v *= cellSize;

        Vector3 td = dir.x != 0 ? Vector3.up : Vector3.right;
        Vector4 tangent = new(td.x, td.y, td.z, 1);
        bool flipWinding = Vector3.Dot(Vector3.Cross(u, v), n) < 0f;

        int a = chunk.vertices.Count;
        AddCapVertex(chunk, o, n, tangent);

        int b = chunk.vertices.Count;
        AddCapVertex(chunk, o + u, n, tangent);

        int c = chunk.vertices.Count;
        AddCapVertex(chunk, o + u + v, n, tangent);

        int d = chunk.vertices.Count;
        AddCapVertex(chunk, o + v, n, tangent);

        AddIndexedCapTriangle(chunk, a, b, c, flipWinding, cellId);
        AddIndexedCapTriangle(chunk, a, c, d, flipWinding, cellId);
    }

    void FaceBasis(
        Vector3Int cell,
        Vector3Int dir,
        out Vector3 o,
        out Vector3 u,
        out Vector3 v,
        out Vector3 n)
    {
        Bounds b = CellBounds(cell);
        n = (Vector3)dir;

        if (dir.x != 0)
        {
            o = new Vector3(dir.x > 0 ? b.max.x : b.min.x, b.min.y, b.min.z);
            u = Vector3.up;
            v = Vector3.forward;
        }
        else if (dir.y != 0)
        {
            o = new Vector3(b.min.x, dir.y > 0 ? b.max.y : b.min.y, b.min.z);
            u = Vector3.right;
            v = Vector3.forward;
        }
        else
        {
            o = new Vector3(b.min.x, b.min.y, dir.z > 0 ? b.max.z : b.min.z);
            u = Vector3.right;
            v = Vector3.up;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void AddCapVertex(RuntimeChunk chunk, Vector3 p, Vector3 n, Vector4 tangent)
    {
        chunk.vertices.Add(p);
        chunk.normals.Add(n);

        if (hasTangents)
            chunk.tangents.Add(tangent);
        if (hasUV)
            chunk.uv.Add(Vector2.zero);
        if (hasUV2)
            chunk.uv2.Add(Vector2.zero);
    }

    // =========================================================
    // DAMAGE SHAPES
    // =========================================================

    int RemoveCube(Vector3Int center)
    {
        int half = removeSize / 2;
        int removed = 0;

        for (int z = -half; z <= half; z++)
        for (int y = -half; y <= half; y++)
        for (int x = -half; x <= half; x++)
        {
            if (RemoveCell(center + new Vector3Int(x, y, z)))
                removed++;
        }

        return removed;
    }

    int RemoveIrregularExplosion(Vector3Int center, Vector3 surfaceNormal)
    {
        if (!Valid(center))
            return 0;

        Vector3 inward = -surfaceNormal.normalized;

        if (inward.sqrMagnitude < .5f)
            inward = Vector3.forward;

        BuildBasis(inward, out Vector3 right, out Vector3 up);

        float baseRadius = Mathf.Max(cellSize * .75f, removeSize * cellSize * .5f);

        float rx = baseRadius * UnityEngine.Random.Range(.9f, 1.15f);
        float ry = baseRadius * UnityEngine.Random.Range(.75f, 1.05f);
        float rz = baseRadius * explosionDepthScale * UnityEngine.Random.Range(.9f, 1.1f);

        Vector3 mainCenter =
            CellCenter(center) +
            inward * (rz * explosionInset);

        Vector3 lobe1Offset =
            (right * UnityEngine.Random.Range(-1f, 1f) * rx * explosionLobeStrength) +
            (up * UnityEngine.Random.Range(-.75f, .75f) * ry * explosionLobeStrength) +
            (inward * UnityEngine.Random.Range(-.2f, .35f) * rz * explosionLobeStrength);

        Vector3 lobe2Offset =
            (right * UnityEngine.Random.Range(-1f, 1f) * rx * explosionLobeStrength) +
            (up * UnityEngine.Random.Range(-.75f, .75f) * ry * explosionLobeStrength) +
            (inward * UnityEngine.Random.Range(-.2f, .35f) * rz * explosionLobeStrength);

        float lobe1Scale = UnityEngine.Random.Range(.45f, .7f);
        float lobe2Scale = UnityEngine.Random.Range(.35f, .6f);

        int reach = Mathf.CeilToInt(Mathf.Max(rx, Mathf.Max(ry, rz)) / cellSize) + 3;
        int seed = UnityEngine.Random.Range(-100000, 100000);

        int removed = 0;

        for (int z = -reach; z <= reach; z++)
        for (int y = -reach; y <= reach; y++)
        for (int x = -reach; x <= reach; x++)
        {
            Vector3Int c = center + new Vector3Int(x, y, z);

            if (!Valid(c))
                continue;

            int cellId = Index(c);
            Cell cell = cells[cellId];

            if (!cell.solid || cell.removed)
                continue;

            Vector3 p = CellCenter(c);

            float m0 = EllipsoidMetric(p, mainCenter, right, up, inward, rx, ry, rz);
            float m1 = EllipsoidMetric(
                p,
                mainCenter + lobe1Offset,
                right,
                up,
                inward,
                rx * lobe1Scale,
                ry * lobe1Scale,
                rz * Mathf.Lerp(lobe1Scale, 1f, .35f)
            );
            float m2 = EllipsoidMetric(
                p,
                mainCenter + lobe2Offset,
                right,
                up,
                inward,
                rx * lobe2Scale,
                ry * lobe2Scale,
                rz * Mathf.Lerp(lobe2Scale, 1f, .35f)
            );

            float metric = Mathf.Min(m0, Mathf.Min(m1, m2));

            float noise = ExplosionNoise(c, seed);
            float threshold = 1f + (noise - .5f) * 2f * explosionEdgeNoise;

            bool inside = metric <= threshold;

            if (!inside && metric <= 1.12f)
            {
                if (noise > 1f - explosionChipChance)
                    inside = true;
            }

            if (!inside)
                continue;

            if (RemoveCell(c))
                removed++;
        }

        return removed;
    }

    void BuildBasis(Vector3 normal, out Vector3 right, out Vector3 up)
    {
        Vector3 refAxis =
            Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > .9f
                ? Vector3.right
                : Vector3.up;

        right = Vector3.Cross(refAxis, normal).normalized;
        up = Vector3.Cross(normal, right).normalized;
    }

    float EllipsoidMetric(
        Vector3 p,
        Vector3 center,
        Vector3 right,
        Vector3 up,
        Vector3 forward,
        float rx,
        float ry,
        float rz)
    {
        Vector3 d = p - center;

        float x = Vector3.Dot(d, right) / Mathf.Max(rx, .0001f);
        float y = Vector3.Dot(d, up) / Mathf.Max(ry, .0001f);
        float z = Vector3.Dot(d, forward) / Mathf.Max(rz, .0001f);

        return x * x + y * y + z * z;
    }

    float ExplosionNoise(Vector3Int c, int seed)
    {
        float x = c.x + seed * .0013f;
        float y = c.y - seed * .0009f;
        float z = c.z + seed * .0017f;

        float n1 = Mathf.PerlinNoise(x * .37f, y * .37f);
        float n2 = Mathf.PerlinNoise(y * .31f, z * .31f);
        float n3 = Mathf.PerlinNoise(z * .29f, x * .29f);

        return (n1 + n2 + n3) / 3f;
    }

    bool RemoveCell(Vector3Int c)
    {
        if (!Valid(c))
            return false;

        int cellId = Index(c);
        Cell cell = cells[cellId];

        if (!cell.solid || cell.removed)
            return false;

        cell.removed = true;
        cells[cellId] = cell;
        removedThisHit.Add(cellId);
        CellRemoved?.Invoke(cellId);

        MarkDirtyAround(c);
        return true;
    }

    // Snapshot only this extraction, independently of subsequent cuts and the
    // source's lifetime, without retaining its entire grid and bake buffers.
    internal DebrisGeometry CaptureDebris(IReadOnlyList<int> ids)
    {
        var geometry = new DebrisGeometry { materials = sourceMR.sharedMaterials };
        var buffer = new RuntimeChunk();
        foreach (int id in ids)
        {
            var cell = new DebrisGeometry.Cell { coord = CoordFromIndex(id) };
            buffer.Clear();
            if (cells[id].surface) AddSourceSurface(buffer, id);
            cell.surface = CopyDebrisPart(buffer, geometry);
            for (int d = 0; d < 6; d++)
            {
                buffer.Clear();
                if (cells[id].surface) AddCachedCap(buffer, id, cell.coord, d);
                else AddFullCapIndexed(buffer, id, cell.coord, dirs[d]);
                cell.faces[d] = CopyDebrisPart(buffer, geometry);
            }
            cell.center = transform.TransformPoint(gridOrigin + ((Vector3)cell.coord + Vector3.one * .5f) * cellSize);
            geometry.cells.Add(cell);
        }
        return geometry;
    }

    DebrisGeometry.Part CopyDebrisPart(RuntimeChunk buffer, DebrisGeometry geometry)
    {
        var part = new DebrisGeometry.Part { start = geometry.indices.Count, count = buffer.indices.Count };
        int offset = geometry.vertices.Count;
        Matrix4x4 normalMatrix = transform.localToWorldMatrix.inverse.transpose;
        bool mirrored = transform.localToWorldMatrix.determinant < 0;
        for (int i = 0; i < buffer.vertices.Count; i++)
        {
            geometry.vertices.Add(transform.TransformPoint(buffer.vertices[i]));
            geometry.normals.Add(normalMatrix.MultiplyVector(buffer.normals[i]).normalized);
            geometry.uv.Add(hasUV ? buffer.uv[i] : Vector2.zero);
            geometry.uv2.Add(hasUV2 ? buffer.uv2[i] : Vector2.zero);
            Vector4 tangent = hasTangents ? buffer.tangents[i] : new Vector4(1, 0, 0, 1);
            Vector3 direction = transform.TransformVector(new Vector3(tangent.x, tangent.y, tangent.z)).normalized;
            geometry.tangents.Add(new Vector4(direction.x, direction.y, direction.z, mirrored ? -tangent.w : tangent.w));
        }
        for (int i = 0; i < buffer.indices.Count; i += 3)
        {
            geometry.indices.Add(offset + buffer.indices[i]);
            geometry.indices.Add(offset + buffer.indices[i + (mirrored ? 2 : 1)]);
            geometry.indices.Add(offset + buffer.indices[i + (mirrored ? 1 : 2)]);
        }
        return part;
    }

    void MarkDirtyAround(Vector3Int c)
    {
        MarkDirty(c);

        foreach (Vector3Int d in dirs)
            MarkDirty(c + d);
    }

    void MarkDirty(Vector3Int c)
    {
        if (!Valid(c))
            return;

        int id = ChunkId(c);

        if (chunks[id] != null)
            dirtyChunks.Add(id);
    }


}
