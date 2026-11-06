using System;
using System.Collections.Generic;
using UnityEngine;

public class EdgeSpawner : MonoBehaviour
{
    [Min(.001f)] public float spacing = .1f;

    sealed class HoldPlacement
    {
        public Vector3 localPosition;
        public Vector3 localNormal;
        public Vector3 localTangent;
        public GameObject instance;
    }

    sealed class CellHolds
    {
        public readonly List<HoldPlacement> placements = new();
    }

    sealed class ChunkHolds
    {
        public readonly Dictionary<int, CellHolds> cells = new();
        public bool spawned;
    }

    sealed class HoldGroup
    {
        public GameObject source;
        public GridBooleanDestruction grid;
        public readonly Dictionary<int, ChunkHolds> chunks = new();
        public readonly List<GameObject> unowned = new();
        public readonly HashSet<Vector3Int> occupied = new();
        public Action<int> onRemoved;
        public Action onDestroyed;
        public Action<IReadOnlyCollection<int>> onRebuilt;
    }

    readonly Dictionary<GameObject, HoldGroup> groups = new();
    bool started;

    void OnEnable()
    {
        GridBooleanDestruction.Generated += OnGridGenerated;
        if (started) GenerateHoldData();
    }

    void Start()
    {
        started = true;
        GenerateHoldData();
    }

    void GenerateHoldData()
    {
        EdgeSolver solver = FindAnyObjectByType<EdgeSolver>();
        if (!solver) return;
        solver.RecalculateEdges();
        foreach (var entry in EdgeSolver.objectEdges)
            BuildForObject(entry.Key, entry.Value);
    }

    void OnGridGenerated(GridBooleanDestruction grid)
    {
        if (!started || groups.ContainsKey(grid.gameObject) ||
            grid.gameObject.layer != LayerMask.NameToLayer("Ground")) return;
        EdgeSolver solver = FindAnyObjectByType<EdgeSolver>();
        MeshFilter source = grid.GetComponent<MeshFilter>();
        if (!solver || !source || !source.sharedMesh) return;
        BuildForObject(grid.gameObject,
            EdgeSolver.CalculateEdges(source.sharedMesh, solver.calculateOnlyTop));
    }

    void BuildForObject(GameObject source, List<Edge> edges)
    {
        if (!source || groups.ContainsKey(source)) return;
        GridBooleanDestruction grid = source.GetComponent<GridBooleanDestruction>();
        if (grid && !grid.IsReady) return;

        HoldGroup group = new() { source = source, grid = grid };
        groups.Add(source, group);
        if (grid)
        {
            group.onRemoved = id => RemoveCellHolds(group, id);
            group.onDestroyed = () => ReleaseGroup(source, group);
            group.onRebuilt = ids => RefreshChunks(group, ids);
            grid.CellRemoved += group.onRemoved;
            grid.Destroying += group.onDestroyed;
            grid.ChunksRebuilt += group.onRebuilt;
            // Use the current shell, including any cuts made while this spawner
            // was disabled. Never resurrect holds from the original source mesh.
            RefreshChunks(group, new List<int>(grid.RuntimeChunkIds));
            return;
        }

        Transform sourceTransform = source.transform;
        HashSet<Vector3> placed = new();
        float step = Mathf.Max(.001f, spacing);
        foreach (Edge edge in edges)
        {
            float length = Vector3.Distance(sourceTransform.TransformPoint(edge.point1),
                sourceTransform.TransformPoint(edge.point2));
            int intervals = Mathf.FloorToInt(length / step);
            int count = intervals > 0 ? intervals + 1 : 1;
            for (int i = 0; i < count; i++)
            {
                float t = intervals > 0 ? (float)i / intervals : .5f;
                Vector3 localPosition = Vector3.Lerp(edge.point1, edge.point2, t);
                if (!placed.Add(localPosition)) continue;

                Vector3 normal = TransformNormal(sourceTransform, edge.normal);
                Vector3 tangent = sourceTransform.TransformVector(edge.point2 - edge.point1).normalized;
                if (normal.sqrMagnitude > .001f)
                    group.unowned.Add(SpawnHold(sourceTransform, localPosition, normal, tangent));
            }
        }
    }

    ShellEdgeJobs shellJobs;

    struct OwnedHold
    {
        public int cellId;
        public Vector3 position, normal, tangent;
    }
    List<Edge> CalculateShellEdges(List<Vector3> vertices, List<int> indices, bool onlyTop,
        Matrix4x4 matrix)
    {
        shellJobs ??= new ShellEdgeJobs();
        return shellJobs.Calculate(vertices, indices, onlyTop, matrix);
    }

    List<OwnedHold> PlanHolds(HoldGroup group, int chunkId, List<Edge> edges,
        HashSet<Vector3Int> occupied)
    {
        var result = new List<OwnedHold>();
        float step = Mathf.Max(.001f, spacing);
        foreach (Edge edge in edges)
        {
            float length = group.source.transform.TransformVector(edge.point2 - edge.point1).magnitude;
            int intervals = Mathf.FloorToInt(length / step);
            int count = intervals > 0 ? intervals + 1 : 1;
            for (int i = 0; i < count; i++)
            {
                Vector3 position = Vector3.Lerp(edge.point1, edge.point2, intervals > 0 ? (float)i / intervals : .5f);
                if (!group.grid.TryGetHoldCell(position, edge.normal, out int cellId)) continue;
                if (group.grid.GetChunkIdForCell(cellId) != chunkId) continue;
                if (!occupied.Add(ShellEdgeCalculator.Quantize(position))) continue;
                result.Add(new OwnedHold { cellId = cellId, position = position, normal = edge.normal,
                    tangent = (edge.point2 - edge.point1).normalized });
            }
        }
        return result;
    }

    void RefreshChunks(HoldGroup group, IReadOnlyCollection<int> ids)
    {
        EdgeSolver solver = FindAnyObjectByType<EdgeSolver>();
        bool onlyTop = solver && solver.calculateOnlyTop;
        var vertices = new List<Vector3>();
        var indices = new List<int>();
        var loaded = new HashSet<int>();
        // Release the batch before rebuilding, preserving seam deduplication.
        foreach (int chunkId in ids)
        {
            group.chunks.TryGetValue(chunkId, out ChunkHolds old);
            if (old != null && old.spawned) loaded.Add(chunkId);
            if (old != null)
                foreach (CellHolds cell in old.cells.Values)
                foreach (HoldPlacement placement in cell.placements)
                {
                    ReleaseHold(placement.instance);
                    group.occupied.Remove(ShellEdgeCalculator.Quantize(placement.localPosition));
                }
        }
        foreach (int chunkId in ids)
        {
            var chunk = new ChunkHolds();
            group.chunks[chunkId] = chunk;
            if (group.grid.HasChunkShell(chunkId))
            {
                group.grid.BuildHoldShell(chunkId, vertices, indices);
                List<Edge> edges = CalculateShellEdges(vertices, indices, onlyTop,
                    group.source.transform.localToWorldMatrix);
                List<OwnedHold> plan = PlanHolds(group, chunkId, edges, group.occupied);
                foreach (OwnedHold placement in plan)
                {
                    if (!chunk.cells.TryGetValue(placement.cellId, out CellHolds cell))
                    {
                        cell = new CellHolds();
                        chunk.cells.Add(placement.cellId, cell);
                    }
                    cell.placements.Add(new HoldPlacement { localPosition = placement.position,
                        localNormal = placement.normal, localTangent = placement.tangent });
                }
            }
            if (loaded.Contains(chunkId)) SpawnCellsInChunk(group.grid, chunkId);
        }
    }

    public void SpawnCellsInChunk(GridBooleanDestruction grid, int chunkId)
    {
        if (!TryGetChunk(grid, chunkId, out HoldGroup group, out ChunkHolds chunk) || chunk.spawned)
            return;
        if (!CanUseHoldPool()) return;

        Transform sourceTransform = group.source.transform;
        foreach (CellHolds cell in chunk.cells.Values)
        foreach (HoldPlacement placement in cell.placements)
        {
            if (placement.instance) continue;
            Vector3 normal = TransformNormal(sourceTransform, placement.localNormal);
            Vector3 tangent = sourceTransform.TransformVector(placement.localTangent).normalized;
            if (normal.sqrMagnitude > .001f)
                placement.instance = SpawnHold(sourceTransform, placement.localPosition, normal, tangent);
        }
        chunk.spawned = true;
    }

    public void DespawnCellsInChunk(GridBooleanDestruction grid, int chunkId)
    {
        if (!TryGetChunk(grid, chunkId, out _, out ChunkHolds chunk) || !chunk.spawned)
            return;
        foreach (CellHolds cell in chunk.cells.Values)
        foreach (HoldPlacement placement in cell.placements)
        {
            ReleaseHold(placement.instance);
            placement.instance = null;
        }
        chunk.spawned = false;
    }

    public void SetChunksNearPosition(Vector3 worldPosition, float radius)
    {
        float radiusSquared = Mathf.Max(0f, radius) * Mathf.Max(0f, radius);
        foreach (HoldGroup group in groups.Values)
        {
            if (!group.grid) continue;
            foreach (var entry in group.chunks)
            {
                Bounds bounds = group.grid.GetChunkWorldBounds(entry.Key);
                if (bounds.SqrDistance(worldPosition) <= radiusSquared)
                    SpawnCellsInChunk(group.grid, entry.Key);
                else
                    DespawnCellsInChunk(group.grid, entry.Key);
            }
        }
    }

    public void DespawnAllChunks()
    {
        foreach (HoldGroup group in groups.Values)
        {
            if (!group.grid) continue;
            foreach (int chunkId in group.chunks.Keys)
                DespawnCellsInChunk(group.grid, chunkId);
        }
    }

    bool TryGetChunk(GridBooleanDestruction grid, int chunkId,
        out HoldGroup group, out ChunkHolds chunk)
    {
        group = null;
        chunk = null;
        return grid && groups.TryGetValue(grid.gameObject, out group) &&
            group.chunks.TryGetValue(chunkId, out chunk);
    }

    static Vector3 TransformNormal(Transform source, Vector3 localNormal) =>
        source.localToWorldMatrix.inverse.transpose.MultiplyVector(localNormal).normalized;

    static bool CanUseHoldPool()
    {
        if (Pooler.poolDictionary != null && Pooler.poolDictionary.ContainsKey("Hold")) return true;
        
        return false;
    }

    static GameObject SpawnHold(Transform parent, Vector3 localPosition, Vector3 worldNormal, Vector3 worldTangent)
    {
        if (!CanUseHoldPool()) return null;
        Vector3 forward = -worldNormal.normalized;
        Vector3 right = Vector3.ProjectOnPlane(worldTangent, forward).normalized;
        Quaternion rotation;
        if (right.sqrMagnitude > .001f)
        {
            Vector3 up = Vector3.Cross(forward, right).normalized;
            // Edge endpoints may be returned in either order. Keep the hold's
            // local up axis facing the world sky rather than randomly inverted.
            if (Vector3.Dot(up, Vector3.up) < 0f)
            {
                right = -right;
                up = -up;
            }
            rotation = Quaternion.LookRotation(forward, up);
        }
        else rotation = Quaternion.LookRotation(forward);
        GameObject hold = Pooler.SpawnFromPool("Hold", parent.TransformPoint(localPosition),
            rotation);
        hold.transform.SetParent(parent, true);
        Debug.DrawRay(hold.transform.position, hold.transform.forward * 0.25f, Color.blue, 10f, false);
        return hold;
    }

    static void ReleaseHold(GameObject hold)
    {
        if (!hold) return;
        hold.transform.SetParent(null, true);
        if (Pooler.Instance && Pooler.poolDictionary != null && Pooler.poolDictionary.ContainsKey("Hold"))
            Pooler.DespawnToPool("Hold", hold);
        else Destroy(hold);
    }

    static void RemoveCellHolds(HoldGroup group, int cellId)
    {
        int chunkId = group.grid.GetChunkIdForCell(cellId);
        if (!group.chunks.TryGetValue(chunkId, out ChunkHolds chunk) ||
            !chunk.cells.TryGetValue(cellId, out CellHolds cell)) return;
        foreach (HoldPlacement placement in cell.placements)
        {
            ReleaseHold(placement.instance);
            group.occupied.Remove(ShellEdgeCalculator.Quantize(placement.localPosition));
        }
        chunk.cells.Remove(cellId);
        // Retain the chunk's proximity/load state through an empty interval.
    }

    void ReleaseGroup(GameObject source, HoldGroup group)
    {
        if (group.grid)
        {
            group.grid.CellRemoved -= group.onRemoved;
            group.grid.Destroying -= group.onDestroyed;
            group.grid.ChunksRebuilt -= group.onRebuilt;
        }
        foreach (ChunkHolds chunk in group.chunks.Values)
        foreach (CellHolds cell in chunk.cells.Values)
        foreach (HoldPlacement placement in cell.placements)
            ReleaseHold(placement.instance);
        foreach (GameObject hold in group.unowned) ReleaseHold(hold);
        group.chunks.Clear();
        group.unowned.Clear();
        group.occupied.Clear();
        groups.Remove(source);
        EdgeSolver.objectEdges.Remove(source);
    }

    void OnDisable()
    {
        shellJobs?.Dispose();
        shellJobs = null;
        GridBooleanDestruction.Generated -= OnGridGenerated;
        foreach (var entry in new List<KeyValuePair<GameObject, HoldGroup>>(groups))
            ReleaseGroup(entry.Key, entry.Value);
    }
}
