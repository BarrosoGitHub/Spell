using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    [SerializeField] protected WeaponStats stats = WeaponStats.Default;
    public WeaponStats Stats => stats;

    public virtual bool Reload() => false;

    public abstract (Controller target, InteractionContext context) TryExecuteAttack(
        Controller attacker,
        Vector3 aimPoint
    );

    public abstract void CancelAttack();
}
