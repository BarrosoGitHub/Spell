using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DebrisManager : MonoBehaviour
{
    public enum FractureStyle { RandomCellGroups, RegularCellGroups }
    [Header("Extraction")]
    [Tooltip("1 keeps the extraction together. Higher values partition all removed cells without losing volume.")]
    [Range(1, 64)] public int pieceCount = 1;
    [Tooltip("Both styles use the exact baked cell faces. Random changes grouping, not the cut geometry.")]
    public FractureStyle fractureStyle = FractureStyle.RandomCellGroups;
    [Min(.001f)] public float massPerCell = 1;
    [Min(0)] public float outwardSpeed = 1;
    [Tooltip("Seconds before cleanup. Zero keeps debris indefinitely.")]
    [Min(0)] public float lifetime = 30;
    [Range(0, 31)] public int debrisLayer;

    [Header("Ground impact defaults (copied to each Debris)")]
    public bool breakOnGroundImpact = true;
    public LayerMask groundLayers = ~0;
    [Min(0)] public float minimumImpactSpeed = 4;
    [Range(0, 1)] public float breakChance = 1;
    [Range(2, 16)] public int piecesOnBreak = 3;
    [Min(0)] public int maximumBreakDepth = 3;
    [Min(0)] public float impactGracePeriod = .15f;
    [Range(0, 1)] public float minimumGroundNormal = .5f;

    readonly HashSet<Debris> active = new();

    public void Generate(GridBooleanDestruction source, IReadOnlyList<int> removedCells)
    {
        if (!isActiveAndEnabled || removedCells.Count == 0) return;
        DebrisGeometry geometry = source.CaptureDebris(removedCells);
        var members = new List<int>(geometry.cells.Count);
        for (int i = 0; i < geometry.cells.Count; i++) members.Add(i);
        Spawn(geometry, members, Matrix4x4.identity, pieceCount, null);
    }

    internal bool Break(Debris parent)
    {
        if (!isActiveAndEnabled || parent.members.Count < 2) return false;
        Matrix4x4 mapping = parent.transform.localToWorldMatrix * parent.originalToLocal;
        Spawn(parent.geometry, parent.members, mapping, parent.piecesOnBreak, parent);
        // Disable immediately so delayed Destroy cannot render/collide alongside children.
        parent.gameObject.SetActive(false);
        Unregister(parent);
        Destroy(parent.gameObject);
        return true;
    }

    void Spawn(DebrisGeometry geometry, List<int> members, Matrix4x4 mapping,
        int count, Debris parent)
    {
        List<List<int>> groups = geometry.Partition(members, count, fractureStyle == FractureStyle.RandomCellGroups);
        Vector3 extractionCenter = Vector3.zero;
        foreach (int id in members) extractionCenter += mapping.MultiplyPoint3x4(geometry.cells[id].center);
        extractionCenter /= members.Count;
        foreach (List<int> group in groups)
        {
            Mesh mesh = geometry.BuildMesh(group, mapping, out Vector3 pivot);
            var go = new GameObject("Debris") { layer = debrisLayer };
            // World-baked vertices support scaled and rotated source objects.
            go.transform.position = pivot;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = geometry.materials;
            var collider = go.AddComponent<BoxCollider>();
            collider.center = mesh.bounds.center;
            Vector3 size = mesh.bounds.size;
            collider.size = new Vector3(Mathf.Max(.001f, size.x), Mathf.Max(.001f, size.y), Mathf.Max(.001f, size.z));
            var body = go.AddComponent<Rigidbody>();
            body.mass = parent ? parent.body.mass * group.Count / members.Count : Mathf.Max(.001f, massPerCell * group.Count);
            var debris = go.AddComponent<Debris>();
            debris.Initialize(this, geometry, group, mesh, mapping, pivot, parent ? parent.Depth + 1 : 0);
            if (parent)
            {
                debris.CopyBreakSettings(parent);
                body.linearVelocity = parent.body.GetPointVelocity(body.worldCenterOfMass);
                body.angularVelocity = parent.body.angularVelocity;
            }
            else
            {
                debris.breakOnGroundImpact = breakOnGroundImpact;
                debris.groundLayers = groundLayers;
                debris.minimumImpactSpeed = minimumImpactSpeed;
                debris.breakChance = breakChance;
                debris.piecesOnBreak = piecesOnBreak;
                debris.maximumBreakDepth = maximumBreakDepth;
                debris.impactGracePeriod = impactGracePeriod;
                debris.minimumGroundNormal = minimumGroundNormal;
                body.linearVelocity = (pivot - extractionCenter).normalized * outwardSpeed;
            }
            active.Add(debris);
            if (lifetime > 0) Destroy(go, lifetime);
        }
    }

    internal void Unregister(Debris debris)
    {
        active.Remove(debris);
    }

    void OnDestroy()
    {
        foreach (Debris debris in active)
            if (debris) { debris.manager = null; Destroy(debris.gameObject); }
        active.Clear();
    }
}
