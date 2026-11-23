using System;
using System.Collections;
using UnityEngine;

public class IceWeapon : Weapon
{
    // Contact returns to this weapon; presentation never dispatches controller attacks.
    public event Action<Vector3, Vector3, float, Func<GameObject, bool>> OnCast;

    [SerializeField, Min(1f)] private float projectileSpeed = 80f;
    [SerializeField, Min(0.01f)] private float range = 50f;
    public override float Range => source != null && source.entity != null
        ? Mathf.Max(0f, source.entity.stats.Range) : range;
    public Vector3 AimPoint => currentAimPoint;
    private static readonly WaitForSeconds MultishotDelay = new WaitForSeconds(0.1f);
    private Vector3 currentAimPoint;
    private float nextCastTime;
    private Controller source;
    private Func<GameObject, bool> contactCallback;

    public override bool TryExecuteAttack(Controller attacker, Vector3 aimPoint)
    {
        currentAimPoint = aimPoint;
        if (!isActiveAndEnabled || attacker == null || attacker.entity == null ||
            attacker.entity.CurrentHealth <= 0f || Time.time < nextCastTime)
            return false;

        if (!attacker.entity.stats.IsInRange(attacker.transform.position, aimPoint)) return false;
        Vector3 direction = aimPoint - transform.position;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
            return false;

        nextCastTime = Time.time + 1f / Mathf.Max(0.01f, attacker.entity.stats.EffectiveAttackSpeed);
        source = attacker;
        contactCallback ??= ConfirmContact;
        OnCast?.Invoke(transform.position, direction.normalized * projectileSpeed, Range, contactCallback);
        float multishotChance = Mathf.Max(0f, attacker.entity.stats.MultishotChance);
        int extraShots = Mathf.FloorToInt(multishotChance / 100f);
        float remainingChance = multishotChance % 100f;
        if (remainingChance > 0f && UnityEngine.Random.Range(0f, 100f) < remainingChance)
            extraShots++;
        if (extraShots > 0)
            StartCoroutine(FireExtraShots(attacker, extraShots));
        return true;
    }

    private IEnumerator FireExtraShots(Controller attacker, int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return MultishotDelay;
            if (!isActiveAndEnabled || attacker == null || !attacker.isActiveAndEnabled ||
                attacker.entity == null || attacker.entity.CurrentHealth <= 0f) yield break;

            if (!attacker.entity.stats.IsInRange(attacker.transform.position, currentAimPoint)) continue;
            Vector3 direction = currentAimPoint - transform.position;
            if (direction.sqrMagnitude <= Mathf.Epsilon) continue;
            OnCast?.Invoke(transform.position, direction.normalized * projectileSpeed, Range, contactCallback);
        }
    }

    private bool ConfirmContact(GameObject other)
    {
        Controller target = other.GetComponentInParent<Controller>();
        if (target != null && target == source) return false;
        if (!isActiveAndEnabled || source == null || !source.isActiveAndEnabled ||
            source.entity == null || source.entity.CurrentHealth <= 0f ||
            target == null || target.entity == null || target.entity.CurrentHealth <= 0f)
            return true;

        if (!source.entity.stats.IsInRange(source.transform.position, target.transform.position)) return true;
        ConfirmAttack(target, new InteractionContext
        {
            Type = InteractionType.Attack
        });
        return true;
    }

    public override void CancelAttack()
    {
        StopAllCoroutines();
        source = null;
    }

    private void OnDisable() => CancelAttack();
}
