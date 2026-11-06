using System;
using System.Collections.Generic;
using UnityEngine;

// Initialization-only planar tessellation. Vertical slabs avoid contour stitching
// and support holes and disconnected regions without a polygon library.
internal sealed class CapContourBaker
{
    const float Tolerance = 0.000001f;

    struct Segment
    {
        public Vector2 a, b;
        public float Y(float x) => a.y + (b.y - a.y) * ((x - a.x) / (b.x - a.x));
    }

    struct Crossing
    {
        public float middle, left, right;
    }

    readonly List<Segment> segments = new();
    readonly List<float> cuts = new();
    readonly List<Crossing> crossings = new();
    readonly Dictionary<Vector2, int> vertexLookup = new();
    readonly Vector3[] intersections = new Vector3[3];
    public readonly List<Vector2> vertices = new();
    public readonly List<int> indices = new();
    public bool IsFull { get; private set; }

    public void Clear()
    {
        segments.Clear();
        cuts.Clear();
        crossings.Clear();
        vertexLookup.Clear();
        vertices.Clear();
        indices.Clear();
        IsFull = false;
    }

    public void AddSegment(Vector2 a, Vector2 b)
    {
        a = new Vector2(Mathf.Clamp01(a.x), Mathf.Clamp01(a.y));
        b = new Vector2(Mathf.Clamp01(b.x), Mathf.Clamp01(b.y));
        if ((a - b).sqrMagnitude <= Tolerance * Tolerance) return;
        if (a.x > b.x || (a.x == b.x && a.y > b.y)) (a, b) = (b, a);
        // Both neighboring surface cells may contribute the same cut line.
        foreach (Segment segment in segments)
            if ((segment.a - a).sqrMagnitude <= Tolerance * Tolerance &&
                (segment.b - b).sqrMagnitude <= Tolerance * Tolerance) return;
        segments.Add(new Segment { a = a, b = b });
    }

    public void AddTriangle(Vector3 a, Vector3 b, Vector3 c,
        Vector3 origin, Vector3 u, Vector3 v, Vector3 normal, float size)
    {
        float da = Vector3.Dot(a - origin, normal);
        float db = Vector3.Dot(b - origin, normal);
        float dc = Vector3.Dot(c - origin, normal);
        // Match the clipping tolerance used by the source-cell geometry.
        const float planeTolerance = .00001f;
        if (Mathf.Abs(da) <= planeTolerance) da = 0;
        if (Mathf.Abs(db) <= planeTolerance) db = 0;
        if (Mathf.Abs(dc) <= planeTolerance) dc = 0;
        if (da == 0 && db == 0 && dc == 0)
        {
            AddProjected(a, b, origin, u, v, size);
            AddProjected(b, c, origin, u, v, size);
            AddProjected(c, a, origin, u, v, size);
            return;
        }
        int count = 0;
        IntersectEdge(a, b, da, db, ref count);
        IntersectEdge(b, c, db, dc, ref count);
        IntersectEdge(c, a, dc, da, ref count);
        if (count >= 2) AddProjected(intersections[0], intersections[1], origin, u, v, size);
    }

    void IntersectEdge(Vector3 a, Vector3 b, float da, float db, ref int count)
    {
        Vector3 point;
        if (da == 0) point = a;
        else if ((da < 0 && db > 0) || (da > 0 && db < 0))
            point = a + (b - a) * (da / (da - db));
        else return;
        for (int i = 0; i < count; i++)
            if ((intersections[i] - point).sqrMagnitude < 1e-16f) return;
        if (count < intersections.Length) intersections[count++] = point;
    }

    void AddProjected(Vector3 a, Vector3 b, Vector3 origin, Vector3 u, Vector3 v, float size)
    {
        AddSegment(new Vector2(Vector3.Dot(a - origin, u) / size, Vector3.Dot(a - origin, v) / size),
            new Vector2(Vector3.Dot(b - origin, u) / size, Vector3.Dot(b - origin, v) / size));
    }

    public void Bake(Func<Vector2, bool> inside)
    {
        cuts.Add(0);
        cuts.Add(1);
        foreach (Segment segment in segments)
        {
            cuts.Add(segment.a.x);
            cuts.Add(segment.b.x);
        }
        // Splitting at intersections also handles overlapping/copanar input
        // edges: their vertical order cannot change inside a slab.
        for (int i = 0; i < segments.Count; i++)
        for (int j = i + 1; j < segments.Count; j++)
        {
            Segment a = segments[i], b = segments[j];
            Vector2 r = a.b - a.a, s = b.b - b.a;
            float denominator = Cross(r, s);
            if (Mathf.Abs(denominator) < 1e-12f) continue;
            float t = Cross(b.a - a.a, s) / denominator;
            float q = Cross(b.a - a.a, r) / denominator;
            if (t > 0 && t < 1 && q > 0 && q < 1) cuts.Add(a.a.x + t * r.x);
        }
        cuts.Sort();
        bool full = true;
        float left = cuts[0];
        for (int i = 1; i < cuts.Count; i++)
        {
            float right = cuts[i];
            if (right - left <= Tolerance) continue;
            float middle = (left + right) * .5f;
            crossings.Clear();
            crossings.Add(new Crossing { middle = 0, left = 0, right = 0 });
            crossings.Add(new Crossing { middle = 1, left = 1, right = 1 });
            foreach (Segment segment in segments)
            {
                if (segment.a.x >= middle || segment.b.x <= middle) continue;
                crossings.Add(new Crossing {
                    middle = Mathf.Clamp01(segment.Y(middle)),
                    left = Mathf.Clamp01(segment.Y(left)),
                    right = Mathf.Clamp01(segment.Y(right))
                });
            }
            crossings.Sort((a, b) => a.middle.CompareTo(b.middle));
            for (int j = 1; j < crossings.Count; j++)
            {
                Crossing lower = crossings[j - 1], upper = crossings[j];
                if (upper.middle - lower.middle <= Tolerance) continue;
                if (!inside(new Vector2(middle, (lower.middle + upper.middle) * .5f)))
                {
                    full = false;
                    continue;
                }
                Vector2 a = new(left, lower.left), b = new(right, lower.right);
                Vector2 c = new(right, upper.right), d = new(left, upper.left);
                AddTriangle(a, b, c);
                AddTriangle(a, c, d);
            }
            left = right;
        }
        IsFull = full && indices.Count > 0;
    }

    void AddTriangle(Vector2 a, Vector2 b, Vector2 c)
    {
        if (Cross(b - a, c - a) <= 1e-12f) return;
        indices.Add(Vertex(a));
        indices.Add(Vertex(b));
        indices.Add(Vertex(c));
    }

    int Vertex(Vector2 point)
    {
        if (vertexLookup.TryGetValue(point, out int index)) return index;
        index = vertices.Count;
        vertices.Add(point);
        vertexLookup.Add(point, index);
        return index;
    }

    static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
}
