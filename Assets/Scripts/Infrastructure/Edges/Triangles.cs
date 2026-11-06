using UnityEngine;

public class Triangles
{
    public Mesh mesh;
    public int[] indexes;
    public Vector3[] vertices;

    public Triangles(Mesh mesh)
    {
        this.mesh = mesh;
        this.indexes = mesh.triangles;
        this.vertices = mesh.vertices;
    }
}
