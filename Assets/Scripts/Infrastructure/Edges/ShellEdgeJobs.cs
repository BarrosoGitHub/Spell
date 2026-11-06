using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

// A native copy of the tolerance-based shell algorithm. Managed Calculate remains
// the independent reference. Jobs never access GameObjects, physics or hold pools.
internal sealed class ShellEdgeJobs : IDisposable
{
    const float Tolerance = .00001f;

    struct Segment
    {
        public Vector3 a, b, normal, center, min, max;
        public int sourceEdge;
    }
    struct MatchedPair
    {
        public int current, other;
    }
    struct Node
    {
        public Vector3 min, max;
        public int start, count, left, right;
    }
    NativeList<Vector3> vertices;
    NativeList<int> indices;
    NativeList<Segment> segments;
    NativeArray<int> order;
    NativeArray<Node> nodes;
    readonly HashSet<(Vector3Int, Vector3Int)> emitted = new();

    internal List<Edge> Calculate(List<Vector3> sourceVertices, List<int> sourceIndices,
        bool onlyTop, Matrix4x4 localToWorld)
    {
        if (!vertices.IsCreated)
        {
            vertices = new NativeList<Vector3>(Allocator.Persistent);
            indices = new NativeList<int>(Allocator.Persistent);
            segments = new NativeList<Segment>(Allocator.Persistent);
        }
        vertices.ResizeUninitialized(sourceVertices.Count);
        indices.ResizeUninitialized(sourceIndices.Count);
        for (int i = 0; i < sourceVertices.Count; i++) vertices[i] = sourceVertices[i];
        for (int i = 0; i < sourceIndices.Count; i++) indices[i] = sourceIndices[i];
        int required = Math.Max(1, sourceIndices.Count);
        if (!order.IsCreated || order.Length < required)
        {
            if (order.IsCreated) order.Dispose();
            if (nodes.IsCreated) nodes.Dispose();
            order = new NativeArray<int>(required, Allocator.Persistent);
            nodes = new NativeArray<Node>(checked(required * 2), Allocator.Persistent);
        }
        if (segments.Capacity < required) segments.Capacity = required;
        new BuildTreeJob
        {
            vertices = vertices.AsArray(), indices = indices.AsArray(),
            segments = segments, order = order, nodes = nodes
        }.Schedule().Complete();
        // Stream entries have one writer per segment and are read in segment order.
        // Its lifetime includes allocation, completion, collection and disposal.
        var result = new List<Edge>();
        if (segments.Length > 0)
        {
            using (var stream = new NativeStream(segments.Length, Allocator.TempJob))
            {
                new MatchJob
                {
                    segments = segments.AsArray(), order = order, nodes = nodes,
                    normalMatrix = localToWorld.inverse.transpose, onlyTop = onlyTop,
                    output = stream.AsWriter()
                }.Schedule(segments.Length, 32).Complete();
                emitted.Clear();
                NativeStream.Reader reader = stream.AsReader();
                var managedMatcher = new MatchJob { normalMatrix = localToWorld.inverse.transpose, onlyTop = onlyTop };
                for (int i = 0; i < segments.Length; i++)
                {
                    int count = reader.BeginForEachIndex(i);
                    for (int j = 0; j < count; j++)
                    {
                        MatchedPair pair = reader.Read<MatchedPair>();
                        // Use original source triangles and managed arithmetic for
                        // placement coordinates. Sub-tolerance Burst drift can cross
                        // a quantization, spacing or cell-ownership boundary.
                        Segment current = SourceSegment(sourceVertices, sourceIndices, pair.current);
                        Segment other = SourceSegment(sourceVertices, sourceIndices, pair.other);
                        if (!managedMatcher.TryMatch(current, other, out Edge edge)) continue;
                        if (emitted.Add((ShellEdgeCalculator.Quantize(edge.point1), ShellEdgeCalculator.Quantize(edge.point2))))
                            result.Add(edge);
                    }
                    reader.EndForEachIndex();
                }
            }
        }
        return result;
    }

    static Segment SourceSegment(List<Vector3> sourceVertices, List<int> sourceIndices, int sourceEdge)
    {
        int triangle = sourceEdge / 3 * 3;
        int corner = sourceEdge % 3;
        Vector3 a = sourceVertices[sourceIndices[triangle]],
            b = sourceVertices[sourceIndices[triangle + 1]],
            c = sourceVertices[sourceIndices[triangle + 2]];
        return new Segment
        {
            a = sourceVertices[sourceIndices[sourceEdge]],
            b = sourceVertices[sourceIndices[triangle + (corner + 1) % 3]],
            normal = Vector3.Cross(b - a, c - a).normalized,
            center = (a + b + c) / 3
        };
    }

    internal static int Differences(List<Edge> a, List<Edge> b)
    {
        int count = Math.Abs(a.Count - b.Count);
        for (int i = 0; i < Math.Min(a.Count, b.Count); i++)
            if (!Near(a[i].point1, b[i].point1) || !Near(a[i].point2, b[i].point2) || !Near(a[i].normal, b[i].normal)) count++;
        return count;
    }
    internal static bool Near(Vector3 a, Vector3 b) => (a - b).sqrMagnitude <= 1e-10f;

    public void Dispose()
    {
        if (vertices.IsCreated) vertices.Dispose();
        if (indices.IsCreated) indices.Dispose();
        if (segments.IsCreated) segments.Dispose();
        if (order.IsCreated) order.Dispose();
        if (nodes.IsCreated) nodes.Dispose();
    }

    struct CenterComparer : IComparer<int>
    {
        public NativeArray<Segment> segments;
        public int axis;
        public int Compare(int a, int b)
        {
            Segment x = segments[a], y = segments[b];
            int value = (x.min[axis] + x.max[axis]).CompareTo(y.min[axis] + y.max[axis]);
            return value != 0 ? value : a.CompareTo(b);
        }
    }

    [BurstCompile(FloatMode = FloatMode.Strict, FloatPrecision = FloatPrecision.Standard)]
    struct BuildTreeJob : IJob
    {
        [ReadOnly] public NativeArray<Vector3> vertices;
        [ReadOnly] public NativeArray<int> indices;
        public NativeList<Segment> segments;
        public NativeArray<int> order;
        public NativeArray<Node> nodes;
        int nodeCount;

        public void Execute()
        {
            segments.Clear();
            for (int i = 0; i < indices.Length; i += 3)
            {
                Vector3 a = vertices[indices[i]], b = vertices[indices[i + 1]], c = vertices[indices[i + 2]];
                Vector3 cross = Vector3.Cross(b - a, c - a);
                if (cross.sqrMagnitude < 1e-16f) continue;
                Vector3 normal = cross.normalized, center = (a + b + c) / 3;
                Add(a, b, normal, center, i); Add(b, c, normal, center, i + 1); Add(c, a, normal, center, i + 2);
            }
            for (int i = 0; i < segments.Length; i++) order[i] = i;
            nodeCount = 0;
            if (segments.Length > 0) Build(0, segments.Length);
        }
        void Add(Vector3 a, Vector3 b, Vector3 normal, Vector3 center, int sourceEdge)
        {
            if ((b - a).sqrMagnitude < Tolerance * Tolerance) return;
            segments.Add(new Segment { a = a, b = b, normal = normal, center = center,
                min = Vector3.Min(a, b), max = Vector3.Max(a, b), sourceEdge = sourceEdge });
        }
        int Build(int start, int count)
        {
            int index = nodeCount++;
            Segment first = segments[order[start]];
            var node = new Node { min = first.min, max = first.max, start = start, count = count };
            for (int i = start + 1; i < start + count; i++)
            {
                Segment segment = segments[order[i]];
                node.min = Vector3.Min(node.min, segment.min); node.max = Vector3.Max(node.max, segment.max);
            }
            if (count > 8)
            {
                Vector3 size = node.max - node.min;
                int axis = size.x >= size.y && size.x >= size.z ? 0 : size.y >= size.z ? 1 : 2;
                order.GetSubArray(start, count).Sort(new CenterComparer { segments = segments.AsArray(), axis = axis });
                int half = count / 2;
                node.left = Build(start, half); node.right = Build(start + half, count - half);
                node.count = 0;
            }
            nodes[index] = node;
            return index;
        }
    }

    [BurstCompile(FloatMode = FloatMode.Strict, FloatPrecision = FloatPrecision.Standard)]
    struct MatchJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Segment> segments;
        [ReadOnly] public NativeArray<int> order;
        [ReadOnly] public NativeArray<Node> nodes;
        public Matrix4x4 normalMatrix;
        public bool onlyTop;
        public NativeStream.Writer output;

        public void Execute(int segmentId)
        {
            output.BeginForEachIndex(segmentId);
            using (var candidates = new NativeList<int>(Allocator.Temp))
            {
                Segment current = segments[segmentId];
                Vector3 padding = Vector3.one * (Tolerance * 2);
                Query(0, current.min - padding, current.max + padding, segmentId, candidates);
                candidates.AsArray().Sort();
                for (int i = 0; i < candidates.Length; i++)
                {
                    Segment other = segments[candidates[i]];
                    if (TryMatch(current, other, out _))
                        output.Write(new MatchedPair { current = current.sourceEdge, other = other.sourceEdge });
                }
            }
            output.EndForEachIndex();
        }
        void Query(int index, Vector3 min, Vector3 max, int before, NativeList<int> candidates)
        {
            Node node = nodes[index];
            if (node.max.x < min.x || node.min.x > max.x || node.max.y < min.y || node.min.y > max.y ||
                node.max.z < min.z || node.min.z > max.z) return;
            if (node.count == 0)
            {
                Query(node.left, min, max, before, candidates); Query(node.right, min, max, before, candidates);
                return;
            }
            for (int i = node.start; i < node.start + node.count; i++)
                if (order[i] < before) candidates.Add(order[i]);
        }
        public bool TryMatch(Segment current, Segment other, out Edge edge)
        {
            edge = default;
            Vector3 a = current.a, b = current.b, normal = current.normal, center = current.center;
            Vector3 direction = (b - a).normalized;
            Vector3 abs = new(Mathf.Abs(direction.x), Mathf.Abs(direction.y), Mathf.Abs(direction.z));
            float dominant = abs.x >= abs.y && abs.x >= abs.z ? direction.x : abs.y >= abs.z ? direction.y : direction.z;
            if (dominant < 0) direction = -direction;
            if (Vector3.Cross(direction, (other.b - other.a).normalized).sqrMagnitude > 1e-8f) return false;
            if (Vector3.Dot(normal, other.normal) > .17364818f) return false;
            if (Vector3.Dot(normal, other.center - center) >= -Tolerance ||
                Vector3.Dot(other.normal, center - other.center) >= -Tolerance) return false;
            float start = Vector3.Dot(a, direction), end = Vector3.Dot(b, direction);
            if (start > end) (start, end) = (end, start);
            float oa = Vector3.Dot(other.a, direction), ob = Vector3.Dot(other.b, direction);
            float lo = Mathf.Max(start, Mathf.Min(oa, ob)), hi = Mathf.Min(end, Mathf.Max(oa, ob));
            if (hi - lo <= Tolerance) return false;
            if (Vector3.Cross(other.a - a, direction).sqrMagnitude > Tolerance * Tolerance * 4 ||
                Vector3.Cross(other.b - a, direction).sqrMagnitude > Tolerance * Tolerance * 4) return false;
            Vector3 combined = (normal + other.normal).normalized;
            if (combined.sqrMagnitude < .001f) return false;
            if (onlyTop)
            {
                float angle = Vector3.Angle(Vector3.down, normalMatrix.MultiplyVector(combined));
                if (angle < 95 || angle > 165) return false;
            }
            Vector3 p = a + direction * (lo - Vector3.Dot(a, direction));
            Vector3 q = a + direction * (hi - Vector3.Dot(a, direction));
            edge = new Edge(p, q) { normal = combined };
            return true;
        }
    }
}
