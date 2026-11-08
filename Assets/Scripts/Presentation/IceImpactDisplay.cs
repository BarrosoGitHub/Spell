using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(IceProjectileDisplay))]
public sealed class IceImpactDisplay : MonoBehaviour
{
    [SerializeField] private IceProjectileDisplay projectileDisplay;
    [SerializeField, Min(1)] private int maxIcicles = 128;
    [SerializeField, Min(0f)] private float embedDepth = 0.0625f;

    [SerializeField, Range(0.1f, 1f)] private float landedSizeMultiplier = 0.8f;
    [Tooltip("Maximum random tilt from the incoming direction, in degrees.")]
    [SerializeField, Range(0f, 30f)] private float angleVariation = 12f;

    private IceParticleDamage collisionSource;
    private ParticleSystem impactParticles;
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
        GameObject impactObject = new GameObject("Embedded Icicles");
        impactObject.transform.SetParent(transform, false);
        impactParticles = impactObject.AddComponent<ParticleSystem>();
        impactParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = impactParticles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startSpeed = 0f;
        main.startRotation3D = true;
        main.startColor = projectileDisplay.ProjectileParticles.main.startColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = maxIcicles;
        main.ringBufferMode = ParticleSystemRingBufferMode.PauseUntilReplaced;
        var emission = impactParticles.emission;
        emission.enabled = false;
        var shape = impactParticles.shape;
        shape.enabled = false;
        var renderer = impactParticles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.mesh = shardRenderer.mesh;
        renderer.sharedMaterials = shardRenderer.sharedMaterials;
        renderer.alignment = ParticleSystemRenderSpace.World;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

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
        if (impactParticles != null)
            impactParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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
        Vector3 position = collision.intersection - rotation * (meshTip * size) + direction * embedDepth;
        impactParticles.Play(false);
        impactParticles.Emit(new ParticleSystem.EmitParams
        {
            position = position,
            velocity = Vector3.zero,
            rotation3D = rotation.eulerAngles,
            startSize = size
        }, 1);
    }

    private void OnDestroy()
    {
        if (impactParticles != null) Destroy(impactParticles.gameObject);
    }
}
