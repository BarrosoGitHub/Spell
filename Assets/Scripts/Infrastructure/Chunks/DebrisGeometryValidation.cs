#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Runs without touching the open scene: Tools > Destruction > Validate Debris Geometry.
internal static class DebrisGeometryValidation
{
    [MenuItem("Tools/Destruction/Validate Debris Geometry")]
    public static void Run()
    {
        UnityEngine.Random.State state = UnityEngine.Random.state;
        try
        {
            UnityEngine.Random.InitState(8371);
            DebrisGeometry geometry = CubeFixture();
            var all = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 };
            for (int mode = 0; mode < 2; mode++)
            for (int count = 1; count <= 12; count++)
            {
                List<List<int>> groups = geometry.Partition(all, count, mode == 1);
                Require(groups.Count == Math.Min(count, all.Count), "Piece count clamp");
                var seen = new HashSet<int>();
                double totalVolume = 0;
                foreach (List<int> group in groups)
                {
                    Require(group.Count > 0, "Empty group");
                    foreach (int id in group) Require(seen.Add(id), "Duplicated cell");
                    Mesh mesh = geometry.BuildMesh(group, Matrix4x4.identity, out _);
                    try
                    {
                        totalVolume += Volume(mesh);
                        CheckClosed(mesh);
                        if (count == 1) Require(mesh.triangles.Length / 3 == 48, "Internal faces not removed");
                    }
                    finally { UnityEngine.Object.DestroyImmediate(mesh); }
                }
                Require(seen.Count == 8, "Lost cells");
                Require(Math.Abs(totalVolume - 8) < .0001, "Partition changed volume");
            }

            // Repeated fracture after moving/rotating each parent must keep the
            // original snapshot aligned with the visible parent on every generation.
            Matrix4x4 mapping = Matrix4x4.identity;
            for (int depth = 0; depth < 3; depth++)
            {
                Mesh parent = geometry.BuildMesh(all, mapping, out Vector3 pivot);
                Matrix4x4 pose = Matrix4x4.TRS(pivot + new Vector3(3, 2, -1), Quaternion.Euler(17, 39, 11), Vector3.one);
                Matrix4x4 next = pose * Matrix4x4.Translate(-pivot) * mapping;
                var expected = new List<Vector3>();
                foreach (Vector3 v in parent.vertices) expected.Add(pose.MultiplyPoint3x4(v));
                Mesh moved = geometry.BuildMesh(all, next, out Vector3 movedPivot);
                try
                {
                    Require(Math.Abs(Volume(moved) - 8) < .0002, "Moved debris volume");
                    Vector3[] actual = moved.vertices;
                    for (int i = 0; i < actual.Length; i++)
                        Require(Vector3.Distance(actual[i] + movedPivot, expected[i]) < .0001f, "Recursive fracture transform");
                    double volume = 0;
                    foreach (List<int> group in geometry.Partition(all, 4, true))
                    {
                        Mesh child = geometry.BuildMesh(group, next, out _);
                        volume += Volume(child);
                        UnityEngine.Object.DestroyImmediate(child);
                    }
                    Require(Math.Abs(volume - 8) < .0002, "Recursive fracture lost volume");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(parent);
                    UnityEngine.Object.DestroyImmediate(moved);
                }
                mapping = next;
            }
            
        }
        finally { UnityEngine.Random.state = state; }
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception("Debris validation: " + message);
    }

    static double Volume(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        double volume = 0;
        for (int i = 0; i < triangles.Length; i += 3)
            volume += Vector3.Dot(vertices[triangles[i]], Vector3.Cross(vertices[triangles[i + 1]], vertices[triangles[i + 2]])) / 6.0;
        return volume;
    }

    static void CheckClosed(Mesh mesh)
    {
        var edges = new Dictionary<(Vector3, Vector3), int>();
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        for (int i = 0; i < triangles.Length; i += 3)
        for (int e = 0; e < 3; e++)
        {
            var edge = (vertices[triangles[i + e]], vertices[triangles[i + (e + 1) % 3]]);
            edges.TryGetValue(edge, out int count);
            edges[edge] = count + 1;
        }
        foreach (var edge in edges)
            Require(edges.TryGetValue((edge.Key.Item2, edge.Key.Item1), out int reverse) && reverse == edge.Value,
                "Open or inconsistently wound surface");
    }

    static DebrisGeometry CubeFixture()
    {
        var geometry = new DebrisGeometry();
        for (int z = 0; z < 2; z++)
        for (int y = 0; y < 2; y++)
        for (int x = 0; x < 2; x++)
        {
            var cell = new DebrisGeometry.Cell { coord = new Vector3Int(x, y, z), center = new Vector3(x, y, z) + Vector3.one * .5f };
            for (int d = 0; d < 6; d++)
            {
                Vector3 normal = DebrisGeometry.Directions[d];
                Vector3 u = d < 2 ? Vector3.up : Vector3.right;
                Vector3 v = Vector3.Cross(normal, u);
                Vector3 center = cell.center + normal * .5f;
                int offset = geometry.vertices.Count;
                geometry.vertices.AddRange(new[] { center - u * .5f - v * .5f, center + u * .5f - v * .5f,
                    center + u * .5f + v * .5f, center - u * .5f + v * .5f });
                for (int i = 0; i < 4; i++)
                {
                    geometry.normals.Add(normal);
                    geometry.tangents.Add(new Vector4(u.x, u.y, u.z, 1));
                    geometry.uv.Add(Vector2.zero);
                    geometry.uv2.Add(Vector2.zero);
                }
                cell.faces[d] = new DebrisGeometry.Part { start = geometry.indices.Count, count = 6 };
                geometry.indices.AddRange(new[] { offset, offset + 1, offset + 2, offset, offset + 2, offset + 3 });
            }
            geometry.cells.Add(cell);
        }
        return geometry;
    }
}
#endif
