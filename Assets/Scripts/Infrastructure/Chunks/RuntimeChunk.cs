using System.Collections.Generic;
using UnityEngine;

class RuntimeChunk
{
    public Vector3Int c;
    public Bounds bounds;

    public Mesh mesh;
    public MeshRenderer renderer;
    public MeshCollider collider;

    public readonly List<Vector3> vertices = new(1024);
    public readonly List<Vector3> normals = new(1024);
    public readonly List<Vector4> tangents = new(1024);
    public readonly List<Vector2> uv = new(1024);
    public readonly List<Vector2> uv2 = new(1024);
    public readonly List<int> indices = new(2048);

    public readonly List<int> triangleOwners = new(1024);

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
}