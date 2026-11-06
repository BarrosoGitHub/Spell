using System.Collections.Generic;
using System;
using UnityEngine;

public class EdgeSolver : MonoBehaviour {
    private GameObject hold;
    public static Dictionary<GameObject, List<Edge>> objectEdges = new Dictionary<GameObject, List<Edge>>();
    public static Dictionary<GameObject, List<BalanceBeam>> balanceBeamsByObjects;
    static Dictionary<Edge, Vector3[]> EdgeDictionary = new Dictionary<Edge, Vector3[]>();
    public bool calculateOnlyTop = false;

    private void Awake() {
        balanceBeamsByObjects = new Dictionary<GameObject, List<BalanceBeam>>();

        // EdgeSpawner coordinates discovery after all grid Awake methods complete.
        // GenerateBalanceBeams();
    }

    public static List<Edge> CalculateEdges(Mesh mesh, bool onlyTop)
    {
        return onlyTop ? edgeCalculationTopOnly(new Triangles(mesh)) : edgeCalculation(new Triangles(mesh));
    }

    public void RecalculateEdges()
    {
        TriangleCalculation(calculateOnlyTop ? edgeCalculationTopOnly : edgeCalculation);
    }

    // [MenuItem("Tools/Edge Script/Recalculate Edges")]
    private void edgeCalculation()
    {
        TriangleCalculation(edgeCalculation);
    }
    // [MenuItem("Tools/Edge Script/Recalculate Only Top Edges")]
    private static void edgeCalculationTopOnly()
    {
        TriangleCalculation(edgeCalculationTopOnly);
    }
    
    public static void TriangleCalculation(Func<Triangles, List<Edge>> edgeCalculationMethod)
    {
        objectEdges.Clear();
        GameObject[] objectsToCalculate = FindGameObjectsInLayer(LayerMask.NameToLayer("Ground"));

        foreach(GameObject obj in objectsToCalculate)
        {
            Mesh mesh = obj.GetComponent<MeshFilter>().sharedMesh;
            
            objectEdges.Add(obj, edgeCalculationMethod(new Triangles(mesh)));         
        }
    }
    private static List<Edge> edgeCalculation(Triangles triangles)
    {
        List<Edge> edgeList = new List<Edge>();

        for (int i = 0; i < triangles.indexes.Length / 3; i++)
        {
            Vector3 p1 = triangles.vertices[triangles.indexes[(i * 3)]];
            Vector3 p2 = triangles.vertices[triangles.indexes[(i * 3) + 1]];
            Vector3 p3 = triangles.vertices[triangles.indexes[(i * 3) + 2]];

            Vector3 midPoint = (p1 + p2 + p3) / 3;

            Edge[] edges = {
                new Edge(p1, p2),
                new Edge(p2, p3),
                new Edge(p3, p1)
            };

            Vector3 normal = GetTriangleNormal(p1, p2, p3);
            
            Vector3[] normalValues;
            for(int j = 0; j < 3; j ++)
            {
                if(EdgeDictionary.TryGetValue(edges[j], out normalValues))
                {
                    float dotProduct = Vector3.Dot(normal, (normalValues[1] - midPoint).normalized);

                    if(dotProduct < 0 && Vector3.Angle(normal, normalValues[0]) >= 80)
                    {
                        edges[j].normal = EdgeDictionary[edges[j]][0] + normal;
                        edgeList.Add(edges[j]);
                    }
                    else
                    {
                        EdgeDictionary.Remove(edges[j]);
                    }
                }
                else
                {
                    EdgeDictionary.Add(edges[j], new Vector3[] {normal, midPoint});
                }
            }
        }

        EdgeDictionary.Clear();

        return edgeList;
    }

    private static void GenerateBalanceBeams()
    {
        float parallelDistanceThreshold = 0.5f;

        foreach (var objectEntry in objectEdges)
        {
            GameObject obj = objectEntry.Key;
            List<Edge> edges = objectEntry.Value;

            List<BalanceBeam> balanceBeams = new List<BalanceBeam>(); 

            for (int i = 0; i < edges.Count; i++)
            {
                for (int j = i + 1; j < edges.Count; j++)
                {
                    Edge edge1 = edges[i];
                    Edge edge2 = edges[j];

                    if (AreEdgesParallel(edge1, edge2) && AreEdgesCloseEnough(edge1, edge2, parallelDistanceThreshold, obj))
                    {
                        Vector3 point1 = (edge1.point1 + edge2.point1) / 2;
                        Vector3 point2 = (edge1.point2 + edge2.point2) / 2;

                        BalanceBeam balanceBeam = new BalanceBeam(point1, point2);

                        balanceBeams.Add(balanceBeam);

                        Debug.DrawLine(obj.transform.TransformPoint(point1), obj.transform.TransformPoint(point2), Color.blue, 100f);
                    }
                }
            }
            balanceBeamsByObjects.Add(obj, balanceBeams);
        }
    }

    private static bool AreEdgesParallel(Edge edge1, Edge edge2) {
        Vector3 direction1 = edge1.point2 - edge1.point1;
        Vector3 direction2 = edge2.point2 - edge2.point1;

        float dotProduct = Vector3.Dot(direction1.normalized, direction2.normalized);
        float angle = Mathf.Acos(dotProduct) * Mathf.Rad2Deg;

        return Mathf.Approximately(angle, 0f) || Mathf.Approximately(angle, 180f);
    }

    private static bool AreEdgesCloseEnough(Edge edge1, Edge edge2, float distanceThreshold, GameObject obj) {
        Vector3 edge1Point1 = edge1.point1;
        Vector3 edge2Point1 = edge2.point1;

        Vector3 transformedEdge1Point1 = obj.transform.TransformPoint(edge1Point1);
        Vector3 transformedEdge2Point1 = obj.transform.TransformPoint(edge2Point1);

        float distance = Vector3.Distance(transformedEdge1Point1, transformedEdge2Point1);
        return distance <= distanceThreshold;
    }


    private static List<Edge> edgeCalculationTopOnly(Triangles triangles) 
    {
         List<Edge> edgeList = new List<Edge>();

        for (int i = 0; i < triangles.indexes.Length / 3; i++)
        {
            Vector3 p1 = triangles.vertices[triangles.indexes[(i * 3)]];
            Vector3 p2 = triangles.vertices[triangles.indexes[(i * 3) + 1]];
            Vector3 p3 = triangles.vertices[triangles.indexes[(i * 3) + 2]];

            Vector3 midPoint = (p1 + p2 + p3) / 3;

            Edge[] edges = {
                new Edge(p1, p2),
                new Edge(p2, p3),
                new Edge(p3, p1)
            };

            Vector3 normal = GetTriangleNormal(p1, p2, p3);
                
            for(int j = 0; j < 3; j ++)
            {
                Vector3[] normalValues;
                if(EdgeDictionary.TryGetValue(edges[j], out normalValues))
                {
                    float dotProduct = Vector3.Dot(normal, (normalValues[1] - midPoint).normalized);

                    if(dotProduct < 0 & Vector3.Angle(normal, normalValues[0]) >= 80)
                    {
                        edges[j].normal  = EdgeDictionary[edges[j]][0] + normal;

                        float holdAngle = Vector3.Angle(-Vector3.up, edges[j].normal);
                        if (holdAngle < 95 || holdAngle > 165) continue;

                        edgeList.Add(edges[j]);
                    }
                    else
                    {
                        EdgeDictionary.Remove(edges[j]);
                    }
                }
                else
                {
                    EdgeDictionary.Add(edges[j], new Vector3[] {normal, midPoint});
                }
            }
        }

        EdgeDictionary.Clear();

        return edgeList;
    }
    private static Vector3 GetTriangleNormal(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        return Vector3.Cross(p2 - p1, p3 - p1).normalized;  
    }
    private static GameObject[] FindGameObjectsInLayer(int layer)
    {
        var filters = FindObjectsByType<MeshFilter>();
        var goList = new System.Collections.Generic.List<GameObject>();
        for (int i = 0; i < filters.Length; i++)
        {
            MeshFilter filter = filters[i];
            GridBooleanDestruction grid = filter.GetComponentInParent<GridBooleanDestruction>();
            if (filter.gameObject.layer == layer && filter.sharedMesh &&
                (!grid || grid.gameObject == filter.gameObject))
            {
                goList.Add(filter.gameObject);
            }
        }
        if (goList.Count == 0)
        {
            return Array.Empty<GameObject>();
        }
        return goList.ToArray();
    }
}
