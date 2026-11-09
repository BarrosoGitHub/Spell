using System;
using UnityEngine;

public class IceWeapon : Weapon
{
    // Contact returns to this weapon; presentation never dispatches controller attacks.
    public event Action<Vector3, Vector3, WeaponStats, Func<GameObject, bool>> OnCast;

    [SerializeField, Min(1f)] private float projectileSpeed = 80f;
    private float nextCastTime;
    private Controller source;
    private Func<GameObject, bool> contactCallback;

    public override bool TryExecuteAttack(Controller attacker, Vector3 aimPoint)
    {
        if (!isActiveAndEnabled || attacker == null || attacker.entity == null ||
            attacker.entity.CurrentHealth <= 0f || Time.time < nextCastTime)
            return false;

        Vector3 direction = aimPoint - transform.position;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
            return false;

        nextCastTime = Time.time + 1f / Mathf.Max(0.01f, stats.fireRate);
        source = attacker;
        contactCallback ??= ConfirmContact;
        OnCast?.Invoke(transform.position, direction.normalized * projectileSpeed, stats, contactCallback);
        return true;
    }

    private bool ConfirmContact(GameObject other)
    {
        Controller target = other.GetComponentInParent<Controller>();
        if (target != null && target == source) return false;
        if (!isActiveAndEnabled || source == null || !source.isActiveAndEnabled ||
            source.entity == null || source.entity.CurrentHealth <= 0f ||
            target == null || target.entity == null || target.entity.CurrentHealth <= 0f)
            return true;

        target.Consciousness -= stats.consciousnessDamage;
        Debug.Log($"IceWeapon confirmed a hit on {target.name}; sending {stats.damagePerBullet:0.##} damage to EntityManager.", this);
        ConfirmAttack(target, new InteractionContext
        {
            Type = InteractionType.Attack,
            DamageOverride = stats.damagePerBullet
        });
        return true;
    }

    public override void CancelAttack() => source = null;

    private void OnDisable() => CancelAttack();
}
