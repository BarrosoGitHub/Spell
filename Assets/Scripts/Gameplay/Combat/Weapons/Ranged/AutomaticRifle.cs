using UnityEngine;

/// <summary>
/// Request a shot every frame while firing. Only accepted shots run hit detection.
/// Weapon tuning comes from WeaponStats; Weapon manages ammunition. Confirms hits through the weapon event and returns whether a shot fired.
/// </summary>
public class AutomaticRifle : RangedWeapon
{
    [Tooltip("Muzzle-flash particle system. Its position is also the shot origin.")]
    [SerializeField] private ParticleSystem muzzle;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Bullet Effects")]
    [SerializeField, Min(1f)] private float tracerSpeed = 120f;
    [SerializeField, Min(0.01f)] private float tracerLength = 1.2f;
    [SerializeField, Min(0.001f)] private float tracerWidth = 0.025f;
    [SerializeField] private Color tracerColor = new Color(1f, 0.8f, 0.35f, 1f);
    [Tooltip("Optional override for the built-in tracer and impact material.")]
    [SerializeField] private Material shotEffectMaterial;

    private Vector3 MuzzleOrigin => muzzle != null ? muzzle.transform.position : transform.position;



    protected override void Awake()
    {
        base.Awake();
        if (muzzle == null)
            return;

        // The imported effect destroys itself on completion by default.
        // Keep all emitters available for subsequent shots instead.
        foreach (ParticleSystem emitter in muzzle.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = emitter.main;
            main.playOnAwake = false;
            main.stopAction = ParticleSystemStopAction.None;
        }

        muzzle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (muzzle != null)
            muzzle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
    public override bool TryExecuteAttack(Controller attacker, Vector3 aimPoint)
    {

        if (!isActiveAndEnabled || attacker == null || attacker.entity == null ||
            attacker.entity.CurrentHealth <= 0f)
            return false;
        Vector3 origin = MuzzleOrigin;
        Vector3 direction = aimPoint - origin;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
            return false;

        direction.Normalize();
        if (!TryFire(Time.time)) return false;

        if (muzzle != null)
        {
            muzzle.gameObject.SetActive(true);
            // Restart the burst for each shot, allowing existing smoke to finish.
            muzzle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            muzzle.Play(true);
        }

        // Ignore the shooter's own colliders, but keep scenery as an obstruction.
        // RaycastAll is used here for clarity; a busy scene can use a reusable buffer.
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, Range, hitMask,
            QueryTriggerInteraction.Ignore);
        RaycastHit closestHit = default;
        bool hasHit = false;
        float closestDistance = float.PositiveInfinity;

        foreach (RaycastHit hit in hits)
        {
            Controller hitController = hit.collider.GetComponentInParent<Controller>();
            if (hitController == attacker || hit.collider.transform.IsChildOf(attacker.transform) ||
                hit.collider.transform.IsChildOf(transform))
                continue;

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestHit = hit;
                hasHit = true;
            }
        }

        Vector3 end = hasHit ? closestHit.point : origin + direction * Range;
        Controller target = hasHit ? closestHit.collider.GetComponentInParent<Controller>() : null;
        BulletShotEffects.Play(origin, end, hasHit ? closestHit.normal : Vector3.zero,
            hasHit && target == null, tracerSpeed, tracerLength, tracerWidth, tracerColor, shotEffectMaterial);

        Debug.DrawLine(origin, end, Color.yellow, 0.1f);
        // Deliver the original contact before damage/state changes can move the rig.
        if (hasHit)
        {
            if (closestHit.collider.TryGetComponent<RagdollHitbox>(out var ragdollHitbox))
                ragdollHitbox.ApplyBulletImpact(closestHit, direction, Stats);
            OnImpact?.Invoke(closestHit, direction);
        }
        if (target != null && target.entity != null && target.entity.CurrentHealth > 0f)
        {
            target.Consciousness -= Stats.consciousnessDamage;
        }
        if (target != null)
        {
            ConfirmAttack(target, new InteractionContext
            {
                Type = InteractionType.Attack,
                DamageOverride = Stats.damagePerBullet
            });
        }
        return true;
    }

    public override void CancelAttack()
    {
        // Each hitscan shot completes immediately; no pending swing/projectile to cancel.
        // Preserve the cooldown when firing is interrupted.
    }
}
