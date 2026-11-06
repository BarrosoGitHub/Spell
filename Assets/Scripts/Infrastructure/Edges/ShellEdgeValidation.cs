#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

internal static class ShellEdgeValidation
{
    [MenuItem("Tools/Destruction/Validate Shell Edges")]
    public static void Run()
    {
        bool previousSynchronous = Unity.Burst.BurstCompiler.Options.EnableBurstCompileSynchronously;
        Unity.Burst.BurstCompiler.Options.EnableBurstCompileSynchronously = true;
        using var jobs = new ShellEdgeJobs();
        List<Edge> Check(List<Vector3> v, List<int> t, bool top, Matrix4x4 matrix)
        {
            List<Edge> managed = ShellEdgeCalculator.Calculate(v, t, top, matrix);
            List<Edge> burst = jobs.Calculate(v, t, top, matrix);
            Require(ShellEdgeJobs.Differences(managed, burst) == 0, "Managed/Burst edge mismatch");
            for (int edgeId = 0; edgeId < managed.Count; edgeId++)
            {
                Edge a = managed[edgeId], b = burst[edgeId];
                int stepsA = Mathf.FloorToInt(Vector3.Distance(a.point1, a.point2) / .1f);
                int stepsB = Mathf.FloorToInt(Vector3.Distance(b.point1, b.point2) / .1f);
                Require(stepsA == stepsB, "Managed/Burst hold spacing count differs");
                for (int i = 0; i <= stepsA; i++)
                {
                    float fraction = stepsA > 0 ? (float)i / stepsA : .5f;
                    Vector3 pa = Vector3.Lerp(a.point1, a.point2, fraction);
                    Vector3 pb = Vector3.Lerp(b.point1, b.point2, fraction);
                    Require(ShellEdgeCalculator.Quantize(pa) == ShellEdgeCalculator.Quantize(pb),
                        $"Hold occupancy key differs at edge {edgeId}, sample {i}: {pa.ToString("G9")} / {pb.ToString("G9")}");
                }
            }
            return managed;
        }
        try
        {
            // One long top edge meets two short side edges at a T junction.
            var vertices = new List<Vector3> {
                new(0,0,0), new(2,0,0), new(0,0,-1),
                new(0,0,0), new(0,-1,0), new(1,0,0),
                new(1,0,0), new(1,-1,0), new(2,0,0)
            };
            var indices = new List<int> { 0,1,2,3,4,5,6,7,8 };
            var edges = Check(vertices, indices, false, Matrix4x4.identity);
            Require(edges.Count == 2, "T-junction ledges must survive unequal subdivision");
            float length = 0;
            foreach (Edge edge in edges)
            {
                length += Vector3.Distance(edge.point1, edge.point2);
                Require(edge.normal.y > 0 && edge.normal.z > 0, "Outward ledge normal");
            }
            Require(Mathf.Abs(length - 2) < .0001f, "Entire shared edge must be covered");
            Require(Check(vertices, indices, true, Matrix4x4.identity).Count == 2,
                "Top edges must survive filtering");
            Require(Check(vertices, indices, true,
                Matrix4x4.Rotate(Quaternion.Euler(180, 0, 0))).Count == 0, "Top filtering must use world orientation");
            // A sub-micron rounding difference straddles the old line-key boundary.
            // These edges are within the geometric tolerance and must still meet.
            var rounded = new List<Vector3>(vertices);
            for (int i = 0; i < rounded.Count; i++)
                rounded[i] += Vector3.up * (i < 3 ? .0000049f : .0000051f);
            Require(Check(rounded, indices, false, Matrix4x4.identity).Count == 2,
                "Near-coincident edges must match across rounding boundaries");
            // Rotated, translated triangles model clipping a sloped source surface.
            for (int angle = 7; angle < 180; angle += 13)
            {
                Matrix4x4 pose = Matrix4x4.TRS(new Vector3(3.25f, -2.5f, 1.125f),
                    Quaternion.Euler(angle, angle * .73f, angle * .31f), Vector3.one);
                var transformed = new List<Vector3>();
                foreach (Vector3 point in vertices) transformed.Add(pose.MultiplyPoint3x4(point));
                var rotatedEdges = Check(transformed, indices, false, Matrix4x4.identity);
                Require(rotatedEdges.Count == 2, $"Rotated T-junction at {angle} degrees lost an edge");
                float rotatedLength = 0;
                foreach (Edge edge in rotatedEdges) rotatedLength += Vector3.Distance(edge.point1, edge.point2);
                Require(Mathf.Abs(rotatedLength - 2) < .0001f, "Rotated edge coverage");
            }
            for (int sample = 0; sample < 128; sample++)
            {
                Matrix4x4 pose = Matrix4x4.TRS(new Vector3(3.250005f, -2.500005f, 1.125005f),
                    Quaternion.Euler(sample * 1.37f, sample * .73f, sample * .31f), new Vector3(1.7f, .8f, 1.2f));
                var sloped = new List<Vector3>();
                foreach (Vector3 point in vertices) sloped.Add(pose.MultiplyPoint3x4(point));
                Check(sloped, indices, false, Matrix4x4.identity);
            }
            var separated = new List<Vector3>(vertices);
            for (int i = 3; i < separated.Count; i++) separated[i] += Vector3.up * .001f;
            Require(Check(separated, indices, false, Matrix4x4.identity).Count == 0,
                "Parallel edges outside tolerance must not be joined");
            // Reverse the faces and triangle sides: the same line is now concave.
            for (int i = 0; i < indices.Count; i += 3)
                (indices[i + 1], indices[i + 2]) = (indices[i + 2], indices[i + 1]);
            Require(Check(vertices, indices, false, Matrix4x4.identity).Count == 0,
                "Concave joins must not produce holds");
            var flat = new List<Vector3> { Vector3.zero, Vector3.right, Vector3.up, Vector3.one - Vector3.forward };
            Require(Check(flat, new List<int> { 0,1,2,1,3,2 }, false, Matrix4x4.identity).Count == 0,
                "Flat triangulation diagonals must not produce holds");
            
            Check(new List<Vector3>(), new List<int>(), false, Matrix4x4.identity);
            Check(new List<Vector3> { Vector3.zero, Vector3.zero, Vector3.zero }, new List<int> { 0, 1, 2 }, false, Matrix4x4.identity);
            var manyVertices = new List<Vector3>();
            var manyIndices = new List<int>();
            for (int copy = 0; copy < 256; copy++)
            {
                int first = manyVertices.Count;
                Vector3 offset = new Vector3((copy % 16) * 4, (copy / 16) * 4, 0);
                foreach (Vector3 point in vertices) manyVertices.Add(point + offset);
                for (int i = 0; i < 9; i++) manyIndices.Add(first + i);
            }
            Require(Check(manyVertices, manyIndices, true, Matrix4x4.identity).Count == 512, "Parallel matching and large tree coverage");
            Check(flat, new List<int> { 0, 1, 2, 1, 3, 2 }, false, Matrix4x4.identity);
            
        }
        finally { Unity.Burst.BurstCompiler.Options.EnableBurstCompileSynchronously = previousSynchronous; }
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception("Shell edge validation: " + message);
    }
}
#endif
