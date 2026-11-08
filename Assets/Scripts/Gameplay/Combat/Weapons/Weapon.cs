using System;
using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    [SerializeField] protected WeaponStats stats = WeaponStats.Default;
    public WeaponStats Stats => stats;
    public event Action<Controller, InteractionContext> OnAttackConfirmed;

    protected void ConfirmAttack(Controller target, InteractionContext context)
    {
        OnAttackConfirmed?.Invoke(target, context);
    }

    public virtual bool Reload() => false;

    public abstract bool TryExecuteAttack(
        Controller attacker,
        Vector3 aimPoint
    );

    public abstract void CancelAttack();
}
