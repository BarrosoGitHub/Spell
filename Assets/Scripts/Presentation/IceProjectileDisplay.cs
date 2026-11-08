using System;
using UnityEngine;

[RequireComponent(typeof(IceWeapon))]
public sealed class IceProjectileDisplay : MonoBehaviour
{
    [SerializeField] private IceWeapon weapon;
    [SerializeField] private ParticleSystem iceLancePrefab;
    [SerializeField, Min(0.01f)] private float effectScale = 0.25f;
    [Tooltip("ParticlePack's lance points along negative X.")]
    [SerializeField] private Vector3 shardRotationOffset = new Vector3(0f, 90f, 0f);
    [SerializeField] private LayerMask hitMask = Physics.DefaultRaycastLayers;

    private ParticleSystem iceLance;
    private IceParticleDamage particleDamage;
    public ParticleSystem ProjectileParticles => iceLance;
    public IceParticleDamage CollisionDamage => particleDamage;
    public Vector3 ShardRotationOffset => shardRotationOffset;

    private void Awake()
    {
        if (weapon == null) weapon = GetComponent<IceWeapon>();
        if (iceLancePrefab == null)
        {
            Debug.LogError("IceProjectileDisplay requires ParticlePack's IceLance prefab.", this);
            enabled = false;
            return;
        }
        iceLance = Instantiate(iceLancePrefab, transform);
        iceLance.transform.localPosition = Vector3.zero;
        iceLance.transform.localRotation = Quaternion.identity;
        iceLance.transform.localScale = Vector3.one;
        iceLance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        foreach (ParticleSystem emitter in iceLance.GetComponentsInChildren<ParticleSystem>(true))
        {
            var settings = emitter.main;
            settings.playOnAwake = false;
            settings.prewarm = false;
            settings.loop = false;
            settings.useUnscaledTime = false;
            settings.stopAction = ParticleSystemStopAction.None;
            settings.startSizeMultiplier *= effectScale;
            settings.startSpeedMultiplier *= effectScale;
        }
        // Keep the lance and its particle children, but omit the pack's casting sphere.
        foreach (MeshRenderer mesh in iceLance.GetComponentsInChildren<MeshRenderer>(true))
            mesh.enabled = false;
        var main = iceLance.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        main.maxParticles = 64;
        main.startRotation3D = true;
        var shardRenderer = iceLance.GetComponent<ParticleSystemRenderer>();
        shardRenderer.alignment = ParticleSystemRenderSpace.World;
        var rotation = iceLance.rotationOverLifetime;
        rotation.enabled = false;
        var rotationBySpeed = iceLance.rotationBySpeed;
        rotationBySpeed.enabled = false;
        var velocity = iceLance.velocityOverLifetime;
        velocity.enabled = false;
        var emission = iceLance.emission;
        emission.enabled = false;
        var shape = iceLance.shape;
        shape.enabled = false;
        var collision = iceLance.collision;
        collision.collidesWith = hitMask;
        collision.sendCollisionMessages = true;
        collision.lifetimeLoss = 1f;

        particleDamage = iceLance.gameObject.AddComponent<IceParticleDamage>();
    }

    private void OnEnable()
    {
        weapon.OnCast += PlayCast;
    }

    private void PlayCast(Vector3 origin, Vector3 velocity, WeaponStats stats, Func<GameObject, bool> confirmContact)
    {
        particleDamage.Initialize(iceLance, confirmContact);
        var authoredRoll = iceLance.main.startRotationX;
        float roll = UnityEngine.Random.Range(authoredRoll.constantMin, authoredRoll.constantMax) * Mathf.Rad2Deg;
        Quaternion orientation = Quaternion.LookRotation(velocity.normalized) *
            Quaternion.Euler(shardRotationOffset) * Quaternion.AngleAxis(roll, Vector3.right);
        iceLance.Play(false);
        iceLance.Emit(new ParticleSystem.EmitParams
        {
            position = origin,
            velocity = velocity,
            rotation3D = orientation.eulerAngles,
            startLifetime = stats.range / Mathf.Max(1f, velocity.magnitude)
        }, 1);
    }

    private void OnDisable()
    {
        if (weapon != null) weapon.OnCast -= PlayCast;
        if (iceLance != null) iceLance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (particleDamage != null) particleDamage.Initialize(null, null);
    }

    private void OnDestroy()
    {
        if (iceLance != null) Destroy(iceLance.gameObject);
    }
}
