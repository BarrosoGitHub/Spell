using UnityEngine;

public struct Edge {
    public Vector3 point1;
    public Vector3 point2;
    public Vector3 normal;

    public Edge(Vector3 p1, Vector3 p2) {
        point1 = new Vector3();
        point2 = new Vector3();
        this.normal = Vector3.zero;

        var swap = IsGreaterThan(p1, p2);
        point1 = swap ? p2 : p1;
        point2 = swap ? p1 : p2;
    }
    
    bool IsGreaterThan(Vector3 v1, Vector3 v2) {
        if (v1.x > v2.x)
            return true;
        if (v1.x == v2.x && v1.y > v2.y)
            return true;
        if (v1.x == v2.x && v1.y == v2.y)
            return v1.z > v2.z;

        return false;
    }
}