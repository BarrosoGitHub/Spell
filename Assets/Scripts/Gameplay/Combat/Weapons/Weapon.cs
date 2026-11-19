using System;
using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    public abstract float Range { get; }
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
