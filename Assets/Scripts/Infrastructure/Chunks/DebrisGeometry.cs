using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Immutable extraction snapshot shared by all descendants of a hit.
internal sealed class DebrisGeometry
{
    internal struct Part { public int start, count; }
    internal sealed class Cell
    {
        public Vector3Int coord;
        public Vector3 center;
        public Part surface;
        public readonly Part[] faces = new Part[6];
    }

    internal static readonly Vector3Int[] Directions = {
        Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down,
        Vector3Int.forward, Vector3Int.back
    };
    internal readonly List<Cell> cells = new();
    internal readonly List<Vector3> vertices = new(), normals = new();
    internal readonly List<Vector4> tangents = new();
    internal readonly List<Vector2> uv = new(), uv2 = new();
    internal readonly List<int> indices = new();
    internal Material[] materials;

    internal Mesh BuildMesh(List<int> members, Matrix4x4 mapping, out Vector3 pivot)
    {
        Vector3 center = Vector3.zero;
        foreach (int id in members) center += mapping.MultiplyPoint3x4(cells[id].center);
        center /= members.Count;
        pivot = center;
        var membership = new HashSet<Vector3Int>();
        foreach (int id in members) membership.Add(cells[id].coord);
        var positions = new List<Vector3>();
        var ns = new List<Vector3>();
        var ts = new List<Vector4>();
        var uvs = new List<Vector2>();
        var secondUvs = new List<Vector2>();
        var triangles = new List<int>();
        var remap = new Dictionary<int, int>();
        Matrix4x4 normalMapping = mapping.inverse.transpose;

        void Append(Part part)
        {
            for (int i = part.start; i < part.start + part.count; i++)
            {
                int original = indices[i];
                if (!remap.TryGetValue(original, out int mapped))
                {
                    mapped = positions.Count;
                    remap.Add(original, mapped);
                    positions.Add(mapping.MultiplyPoint3x4(vertices[original]) - center);
                    ns.Add(normalMapping.MultiplyVector(normals[original]).normalized);
                    Vector4 t = tangents[original];
                    Vector3 direction = mapping.MultiplyVector(new Vector3(t.x, t.y, t.z)).normalized;
                    ts.Add(new Vector4(direction.x, direction.y, direction.z, t.w));
                    uvs.Add(uv[original]);
                    secondUvs.Add(uv2[original]);
                }
                triangles.Add(mapped);
            }
        }

        foreach (int id in members)
        {
            Cell cell = cells[id];
            Append(cell.surface);
            for (int d = 0; d < 6; d++)
                if (!membership.Contains(cell.coord + Directions[d])) Append(cell.faces[d]);
        }
        var mesh = new Mesh { name = "Extracted debris", indexFormat = positions.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
        mesh.SetVertices(positions);
        mesh.SetNormals(ns);
        mesh.SetTangents(ts);
        mesh.SetUVs(0, uvs);
        mesh.SetUVs(1, secondUvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    // Median splits preserve every cell exactly once and never make empty groups.
    // Random planes vary grouping; fracture surfaces still follow baked cell faces.
    internal List<List<int>> Partition(List<int> members, int count, bool random)
    {
        var groups = new List<List<int>> { new(members) };
        count = Mathf.Clamp(count, 1, members.Count);
        while (groups.Count < count)
        {
            int largest = 0;
            for (int i = 1; i < groups.Count; i++)
                if (groups[i].Count > groups[largest].Count) largest = i;
            List<int> group = groups[largest];
            var bounds = new Bounds(cells[group[0]].center, Vector3.zero);
            foreach (int id in group) bounds.Encapsulate(cells[id].center);
            Vector3 size = bounds.size;
            Vector3 axis = random ? Random.onUnitSphere :
                size.x >= size.y && size.x >= size.z ? Vector3.right :
                size.y >= size.z ? Vector3.up : Vector3.forward;
            group.Sort((a, b) => {
                int comparison = Vector3.Dot(cells[a].center, axis).CompareTo(Vector3.Dot(cells[b].center, axis));
                return comparison != 0 ? comparison : a.CompareTo(b);
            });
            int midpoint = group.Count / 2;
            groups.Add(group.GetRange(midpoint, group.Count - midpoint));
            group.RemoveRange(midpoint, group.Count - midpoint);
        }
        return groups;
    }
}
