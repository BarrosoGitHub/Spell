using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class Debris : MonoBehaviour
{
    [Header("Ground impact breakup")]
    public bool breakOnGroundImpact = true;
    public LayerMask groundLayers = ~0;
    [Min(0)] public float minimumImpactSpeed = 4;
    [Range(0, 1)] public float breakChance = 1;
    [Range(2, 16)] public int piecesOnBreak = 3;
    [Min(0)] public int maximumBreakDepth = 3;
    [Min(0)] public float impactGracePeriod = .15f;
    [Range(0, 1)] public float minimumGroundNormal = .5f;
    [Header("Runtime")]
    [SerializeField] int cellCount;
    [SerializeField] int breakDepth;
    [SerializeField] float lastImpactSpeed;

    internal DebrisManager manager;
    internal DebrisGeometry geometry;
    internal List<int> members;
    internal Matrix4x4 originalToLocal;
    internal Rigidbody body;
    internal int Depth => breakDepth;
    Mesh ownedMesh;
    float born;
    bool breaking;

    internal void Initialize(DebrisManager owner, DebrisGeometry data, List<int> cells,
        Mesh mesh, Matrix4x4 mapping, Vector3 pivot, int depth)
    {
        manager = owner;
        geometry = data;
        members = cells;
        ownedMesh = mesh;
        originalToLocal = Matrix4x4.Translate(-pivot) * mapping;
        cellCount = cells.Count;
        breakDepth = depth;
        body = GetComponent<Rigidbody>();
        born = Time.time;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (breaking || !manager || !breakOnGroundImpact || members.Count < 2 ||
            breakDepth >= maximumBreakDepth || Time.time - born < impactGracePeriod ||
            (groundLayers.value & (1 << collision.gameObject.layer)) == 0 ||
            collision.gameObject.GetComponentInParent<Debris>()) return;
        Vector3 up = Physics.gravity.sqrMagnitude > .0001f ? -Physics.gravity.normalized : Vector3.up;
        float speed = 0;
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);
            if (Vector3.Dot(contact.normal, up) >= minimumGroundNormal)
                speed = Mathf.Max(speed, Mathf.Abs(Vector3.Dot(collision.relativeVelocity, contact.normal)));
        }
        lastImpactSpeed = speed;
        if (speed < minimumImpactSpeed || speed <= 0 || breakChance <= 0 ||
            (breakChance < 1 && Random.value >= breakChance)) return;
        breaking = manager.Break(this);
    }

    internal void CopyBreakSettings(Debris source)
    {
        breakOnGroundImpact = source.breakOnGroundImpact;
        groundLayers = source.groundLayers;
        minimumImpactSpeed = source.minimumImpactSpeed;
        breakChance = source.breakChance;
        piecesOnBreak = source.piecesOnBreak;
        maximumBreakDepth = source.maximumBreakDepth;
        impactGracePeriod = source.impactGracePeriod;
        minimumGroundNormal = source.minimumGroundNormal;
    }

    void OnDestroy()
    {
        if (ownedMesh) Destroy(ownedMesh);
        if (manager) manager.Unregister(this);
    }
}
