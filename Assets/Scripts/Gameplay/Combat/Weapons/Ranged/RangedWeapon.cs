using System;
using UnityEngine;

public abstract class RangedWeapon : Weapon
{
    public Action<RaycastHit, Vector3> OnImpact;

    public int Ammunition => Stats.ammunition;
    public float Range => Stats.range;

    public bool IsReloading { get; private set; }

    private Interpolate reloadTween;

    protected virtual void Awake()
    {
        stats.ammunition = stats.magazine;
        stats.lastAttackTime = float.NegativeInfinity;
    }

    public override bool Reload()
    {
        if (IsReloading || Ammunition == Stats.magazine) return false;

        IsReloading = true;
        reloadTween = gameObject.ToFloat(() => 0f, _ => { }, 1f, Stats.reloadTime)
            .OnComplete(() =>
            {
                stats.ammunition = Stats.magazine;
                IsReloading = false;
                reloadTween = null;
            });
        return true;
    }

    protected bool TryFire(float time)
    {
        if (IsReloading || time < stats.lastAttackTime + 1f / Stats.fireRate) return false;
        if (Ammunition <= 0) { Reload(); return false; }

        stats.ammunition--;
        stats.lastAttackTime = time;
        if (Ammunition == 0) Reload();
        return true;
    }

    protected virtual void OnDisable()
    {
        if (reloadTween != null)
        {
            reloadTween.Cancel();
            reloadTween = null;
        }
        IsReloading = false;
    }
}
