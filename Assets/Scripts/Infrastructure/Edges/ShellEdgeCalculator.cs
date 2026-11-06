using System;
using System.Collections.Generic;
using UnityEngine;

// Cap and source triangles can meet at T junctions. Match overlapping collinear
// segments geometrically; a rounded line key can split an otherwise valid pair.
internal static class ShellEdgeCalculator
{
    const float Tolerance = .00001f;
    readonly struct Segment
    {
        public readonly Vector3 a, b, normal, center, min, max;
        public Segment(Vector3 a, Vector3 b, Vector3 normal, Vector3 center)
        {
            this.a = a; this.b = b; this.normal = normal; this.center = center;
            min = Vector3.Min(a, b); max = Vector3.Max(a, b);
        }
    }

    // Bounding-volume tree avoids comparing every triangle edge with every other
    // edge, while allowing candidates on either side of a rounding boundary.
    sealed class SegmentTree
    {
        struct Node
        {
            public Vector3 min, max;
            public int start, count, left, right;
        }
        sealed class CenterComparer : IComparer<int>
        {
            readonly List<Segment> segments;
            public int axis;
            public CenterComparer(List<Segment> segments) { this.segments = segments; }
            public int Compare(int a, int b)
            {
                Segment x = segments[a], y = segments[b];
                int order = (x.min[axis] + x.max[axis]).CompareTo(y.min[axis] + y.max[axis]);
                return order != 0 ? order : a.CompareTo(b);
            }
        }
        readonly List<Segment> segments;
        readonly int[] order;
        readonly Node[] nodes;
        readonly CenterComparer comparer;
        int nodeCount;

        public SegmentTree(List<Segment> segments)
        {
            this.segments = segments;
            order = new int[segments.Count];
            nodes = new Node[segments.Count * 2];
            comparer = new CenterComparer(segments);
            for (int i = 0; i < order.Length; i++) order[i] = i;
            if (order.Length > 0) Build(0, order.Length);
        }

        int Build(int start, int count)
        {
            int index = nodeCount++;
            Segment first = segments[order[start]];
            var node = new Node { min = first.min, max = first.max, start = start, count = count };
            for (int i = start + 1; i < start + count; i++)
            {
                Segment segment = segments[order[i]];
                node.min = Vector3.Min(node.min, segment.min);
                node.max = Vector3.Max(node.max, segment.max);
            }
            if (count > 8)
            {
                Vector3 size = node.max - node.min;
                comparer.axis = size.x >= size.y && size.x >= size.z ? 0 : size.y >= size.z ? 1 : 2;
                Array.Sort(order, start, count, comparer);
                int half = count / 2;
                node.left = Build(start, half);
                node.right = Build(start + half, count - half);
                node.count = 0;
            }
            nodes[index] = node;
            return index;
        }

        public void FindEarlier(int segmentId, List<int> candidates)
        {
            candidates.Clear();
            Segment segment = segments[segmentId];
            Vector3 padding = Vector3.one * (Tolerance * 2);
            Query(0, segment.min - padding, segment.max + padding, segmentId, candidates);
            // Preserve the original triangle order for duplicate/hold ownership.
            candidates.Sort();
        }

        void Query(int index, Vector3 min, Vector3 max, int before, List<int> candidates)
        {
            Node node = nodes[index];
            if (node.max.x < min.x || node.min.x > max.x ||
                node.max.y < min.y || node.min.y > max.y ||
                node.max.z < min.z || node.min.z > max.z) return;
            if (node.count == 0)
            {
                Query(node.left, min, max, before, candidates);
                Query(node.right, min, max, before, candidates);
                return;
            }
            for (int i = node.start; i < node.start + node.count; i++)
                if (order[i] < before) candidates.Add(order[i]);
        }
    }
    internal static Vector3Int Quantize(Vector3 p) => new(
        Mathf.RoundToInt(p.x / Tolerance), Mathf.RoundToInt(p.y / Tolerance), Mathf.RoundToInt(p.z / Tolerance));

    internal static List<Edge> Calculate(List<Vector3> vertices, List<int> indices,
        bool onlyTop, Matrix4x4 localToWorld)
    {
        var segments = new List<Segment>(indices.Count);
        var result = new List<Edge>();
        var emitted = new HashSet<(Vector3Int, Vector3Int)>();
        Matrix4x4 normalMatrix = localToWorld.inverse.transpose;
        for (int i = 0; i < indices.Count; i += 3)
        {
            Vector3 a = vertices[indices[i]], b = vertices[indices[i + 1]], c = vertices[indices[i + 2]];
            Vector3 cross = Vector3.Cross(b - a, c - a);
            if (cross.sqrMagnitude < 1e-16f) continue;
            Vector3 normal = cross.normalized;
            Vector3 center = (a + b + c) / 3;
            AddSegment(a, b, normal, center);
            AddSegment(b, c, normal, center);
            AddSegment(c, a, normal, center);
        }
        if (segments.Count == 0) return result;
        var tree = new SegmentTree(segments);
        var candidates = new List<int>();
        for (int segmentId = 0; segmentId < segments.Count; segmentId++)
        {
            Segment current = segments[segmentId];
            Vector3 a = current.a, b = current.b, normal = current.normal, center = current.center;
            Vector3 delta = b - a;
            Vector3 direction = delta.normalized;
            // Choose sign using the largest component to avoid near-zero sign noise.
            Vector3 abs = new(Mathf.Abs(direction.x), Mathf.Abs(direction.y), Mathf.Abs(direction.z));
            float dominant = abs.x >= abs.y && abs.x >= abs.z ? direction.x : abs.y >= abs.z ? direction.y : direction.z;
            if (dominant < 0) direction = -direction;
            tree.FindEarlier(segmentId, candidates);
            float start = Vector3.Dot(a, direction), end = Vector3.Dot(b, direction);
            if (start > end) (start, end) = (end, start);
            foreach (int otherId in candidates)
            {
                Segment other = segments[otherId];
                if (Vector3.Cross(direction, (other.b - other.a).normalized).sqrMagnitude > 1e-8f) continue;
                if (Vector3.Dot(normal, other.normal) > .17364818f) continue; // 80 degrees
                if (Vector3.Dot(normal, other.center - center) >= -Tolerance ||
                    Vector3.Dot(other.normal, center - other.center) >= -Tolerance) continue; // concave/flat
                float oa = Vector3.Dot(other.a, direction), ob = Vector3.Dot(other.b, direction);
                float lo = Mathf.Max(start, Mathf.Min(oa, ob)), hi = Mathf.Min(end, Mathf.Max(oa, ob));
                if (hi - lo <= Tolerance) continue;
                // Bounding boxes are only a broad phase. Reject offset/skew edges
                // using the same geometric tolerance as the original calculator.
                if (Vector3.Cross(other.a - a, direction).sqrMagnitude > Tolerance * Tolerance * 4 ||
                    Vector3.Cross(other.b - a, direction).sqrMagnitude > Tolerance * Tolerance * 4) continue;
                Vector3 combined = (normal + other.normal).normalized;
                if (combined.sqrMagnitude < .001f) continue;
                if (onlyTop)
                {
                    float angle = Vector3.Angle(Vector3.down, normalMatrix.MultiplyVector(combined));
                    if (angle < 95 || angle > 165) continue;
                }
                Vector3 p = a + direction * (lo - Vector3.Dot(a, direction));
                Vector3 q = a + direction * (hi - Vector3.Dot(a, direction));
                Edge edge = new(p, q) { normal = combined };
                if (emitted.Add((Quantize(edge.point1), Quantize(edge.point2)))) result.Add(edge);
            }
        }
        return result;

        void AddSegment(Vector3 a, Vector3 b, Vector3 normal, Vector3 center)
        {
            if ((b - a).sqrMagnitude >= Tolerance * Tolerance)
                segments.Add(new Segment(a, b, normal, center));
        }
    }
}
