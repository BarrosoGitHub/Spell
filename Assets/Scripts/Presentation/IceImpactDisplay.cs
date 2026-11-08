using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(IceProjectileDisplay))]
public sealed class IceImpactDisplay : MonoBehaviour
{
    [SerializeField] private IceProjectileDisplay projectileDisplay;
    [SerializeField, Min(1)] private int maxIcicles = 128;
    [SerializeField, Min(0f)] private float embedDepth = 0.2f;

    [SerializeField, Range(0.1f, 1f)] private float landedSizeMultiplier = 0.225f;
    [Tooltip("Maximum random tilt from the incoming direction, in degrees.")]
    [SerializeField, Range(0f, 30f)] private float angleVariation = 12f;

    private IceParticleDamage collisionSource;
    private Transform[] icicles;
    private Mesh shardMesh;
    private Material[] shardMaterials;
    private int nextIcicle;
    private Vector3 meshTip;

    private void Start()
    {
        if (projectileDisplay == null) projectileDisplay = GetComponent<IceProjectileDisplay>();
        if (projectileDisplay.ProjectileParticles == null || projectileDisplay.CollisionDamage == null)
        {
            Debug.LogError("IceImpactDisplay requires an initialized IceProjectileDisplay.", this);
            enabled = false;
            return;
        }

        ParticleSystemRenderer shardRenderer = projectileDisplay.ProjectileParticles.GetComponent<ParticleSystemRenderer>();
        meshTip = new Vector3(shardRenderer.mesh.bounds.min.x, 0f, 0f);
        shardMesh = shardRenderer.mesh;
        shardMaterials = shardRenderer.sharedMaterials;
        icicles = new Transform[Mathf.Max(1, maxIcicles)];

        collisionSource = projectileDisplay.CollisionDamage;
        collisionSource.OnImpact += LeaveIcicle;
    }

    private void OnEnable()
    {
        if (collisionSource != null) collisionSource.OnImpact += LeaveIcicle;
    }

    private void OnDisable()
    {
        if (collisionSource != null) collisionSource.OnImpact -= LeaveIcicle;
        ClearIcicles();
    }

    private void LeaveIcicle(ParticleCollisionEvent collision)
    {
        Vector3 direction = collision.velocity.sqrMagnitude > Mathf.Epsilon
            ? collision.velocity.normalized : -collision.normal;
        if (direction.sqrMagnitude <= Mathf.Epsilon) return;

        Vector2 tilt = Random.insideUnitCircle * angleVariation;
        Quaternion facing = Quaternion.LookRotation(direction) * Quaternion.Euler(tilt.x, tilt.y, 0f);
        Quaternion rotation = facing * Quaternion.Euler(projectileDisplay.ShardRotationOffset);
        var sizeRange = projectileDisplay.ProjectileParticles.main.startSize;
        float size = Random.Range(sizeRange.constantMin, sizeRange.constantMax) * landedSizeMultiplier;
        Vector3 contact = collision.intersection;
        Transform parent = collision.colliderComponent != null ? collision.colliderComponent.transform : transform;
        if (collision.colliderComponent != null &&
            collision.colliderComponent.TryGetComponent<RagdollHitbox>(out var hitbox) &&
            hitbox.VisibleBone != null)
        {
            // Preserve the hit's limb-local pose on the rendered, blended skeleton.
            Transform bone = hitbox.VisibleBone;
            contact = bone.TransformPoint(parent.InverseTransformPoint(contact));
            Quaternion toVisibleBone = bone.rotation * Quaternion.Inverse(parent.rotation);
            rotation = toVisibleBone * rotation;
            direction = toVisibleBone * direction;
            parent = bone;
        }
        Vector3 position = contact - rotation * (meshTip * size) + direction * embedDepth;
        Transform icicle = icicles[nextIcicle];
        if (icicle == null)
        {
            GameObject shard = new GameObject("Embedded Icicle");
            shard.AddComponent<EmbeddedIcicleLifetime>();
            shard.AddComponent<MeshFilter>().sharedMesh = shardMesh;
            var renderer = shard.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = shardMaterials;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            icicle = shard.transform;
            icicles[nextIcicle] = icicle;
        }
        nextIcicle = (nextIcicle + 1) % icicles.Length;
        // Establish world size before parenting so scaled models retain the impact size.
        icicle.SetParent(null, false);
        icicle.SetPositionAndRotation(position, rotation);
        icicle.localScale = Vector3.one * size;
        icicle.SetParent(parent, true);
    }

    private void ClearIcicles()
    {
        if (icicles == null) return;
        for (int i = 0; i < icicles.Length; i++)
        {
            if (icicles[i] != null) Destroy(icicles[i].gameObject);
            icicles[i] = null;
        }
        nextIcicle = 0;
    }

    private void OnDestroy()
    {
        ClearIcicles();
    }
}

// Remove attachments when a pooled target is disabled, before its next spawn.
internal sealed class EmbeddedIcicleLifetime : MonoBehaviour
{
    private void OnDisable()
    {
        Destroy(gameObject);
    }
}
