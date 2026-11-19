using System;
using UnityEngine;

public class IceWeapon : Weapon
{
    // Contact returns to this weapon; presentation never dispatches controller attacks.
    public event Action<Vector3, Vector3, float, Func<GameObject, bool>> OnCast;

    [SerializeField, Min(1f)] private float projectileSpeed = 80f;
    [SerializeField, Min(0.01f)] private float range = 50f;
    public override float Range => range;
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

        nextCastTime = Time.time + 1f / Mathf.Max(0.01f, attacker.entity.stats.EffectiveAttackSpeed);
        source = attacker;
        contactCallback ??= ConfirmContact;
        OnCast?.Invoke(transform.position, direction.normalized * projectileSpeed, range, contactCallback);
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

        ConfirmAttack(target, new InteractionContext
        {
            Type = InteractionType.Attack
        });
        return true;
    }

    public override void CancelAttack() => source = null;

    private void OnDisable() => CancelAttack();
}
