using UnityEngine;
using System;

[Serializable]
public struct WeaponStats
{
    [Min(0.01f)] public float fireRate;
    [Min(0f)] public float damagePerBullet;
    [Min(1)] public int magazine;
    [HideInInspector] public int ammunition;
    [HideInInspector] public float lastAttackTime;
    [Min(0f)] public float reloadTime;
    [Min(0.01f)] public float range;
    [Min(0f)] public float consciousnessDamage;
    [Min(0f)] public float impulse;
    [Min(0.01f)] public float responseTime;
    [Min(0.01f)] public float recoveryTime;
    [Range(0f, 1f)] public float softness;

    public static WeaponStats Default => new WeaponStats
    {
        fireRate = 10f,
        damagePerBullet = 5f,
        magazine = 30,
        ammunition = 30,
        lastAttackTime = float.NegativeInfinity,
        reloadTime = 2f,
        range = 50f,
        consciousnessDamage = 50f,
        impulse = 2f,
        responseTime = 0.06f,
        recoveryTime = 1f,
        softness = 1f
    };
}
