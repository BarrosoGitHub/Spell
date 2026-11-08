using System;
using UnityEngine;

public class CombatHandler : MonoBehaviour
{
    [SerializeField] private Transform weaponSocket;
    [SerializeField] private Vector3 weaponOffset;
    [SerializeField] private Weapon startingWeapon;
    public event Action<Controller, InteractionContext> OnAttackConfirmed;

    public Weapon EquippedWeapon { get; private set; }

    private void Start()
    {
        if (startingWeapon != null)
            EquipWeapon(startingWeapon);
    }

    public void EquipWeapon(Weapon weapon)
    {
        UnequipWeapon();

        if (weapon == null)
            return;

        Transform attachment = weaponSocket != null ? weaponSocket : transform;

        EquippedWeapon = Instantiate(weapon, attachment, false);
        EquippedWeapon.transform.localPosition = weaponOffset;
        if (isActiveAndEnabled) EquippedWeapon.OnAttackConfirmed += ConfirmAttack;
    }

    public void UnequipWeapon()
    {
        if (EquippedWeapon == null)
            return;

        EquippedWeapon.OnAttackConfirmed -= ConfirmAttack;
        EquippedWeapon.CancelAttack();
        Destroy(EquippedWeapon.gameObject);
        EquippedWeapon = null;
    }

    public bool TryExecuteAttack(Controller attacker, Vector3 aimPoint)
    {
        if (EquippedWeapon == null)
            return false;

        return EquippedWeapon.TryExecuteAttack(attacker, aimPoint);
    }

    public bool Reload()
    {
        return EquippedWeapon != null && EquippedWeapon.Reload();
    }   

    public void CancelAttack()
    {
        EquippedWeapon?.CancelAttack();
    }

    private void ConfirmAttack(Controller target, InteractionContext context)
    {
        OnAttackConfirmed?.Invoke(target, context);
    }

    private void OnEnable()
    {
        if (EquippedWeapon != null) EquippedWeapon.OnAttackConfirmed += ConfirmAttack;
    }

    private void OnDisable()
    {
        if (EquippedWeapon != null) EquippedWeapon.OnAttackConfirmed -= ConfirmAttack;
        CancelAttack();
    }
}
