using UnityEngine;

public class Stats
{
    public float MaxHealth { get; private set; }
    public float BaseDamage { get; private set; }
    public float AttackSpeed { get; private set; }
    public float MovementSpeed { get; private set; }
    public float Defense { get; private set; }
    public float CriticalChance { get; private set; }
    public float CriticalDamage { get; private set; }
    public float MultishotChance { get; private set; }
    public float Range { get; private set; }

    public float MaxHealthPerc { get; private set; }
    public float BaseDamagePerc { get; private set; }
    public float AttackSpeedPerc { get; private set; }
    public float DefensePerc { get; private set; }

    public float Damage => BaseDamage * (1f + BaseDamagePerc);
    public float EffectiveAttackSpeed => AttackSpeed * (1f + AttackSpeedPerc);
    public float EffectiveDefense => Defense * (1f + DefensePerc);

    public Stats(float maxHealth = 0,
                 float baseDamage = 0, 
                 float attackSpeed = 0, 
                 float defense = 0, 
                 float criticalChance = 0, 
                 float criticalDamage = 0,
                 float maxHealthPerc = 0,
                 float baseDamagePerc = 0,
                 float attackSpeedPerc = 0,
                 float defensePerc = 0,
                 float movementSpeed = 0,
                 float multishotChance = 0,
                 float range = 0)
    {
        MaxHealth = maxHealth;
        BaseDamage = baseDamage;
        AttackSpeed = attackSpeed;
        MovementSpeed = movementSpeed;
        MultishotChance = multishotChance;
        Range = range;
        Defense = defense;
        CriticalChance = criticalChance;
        CriticalDamage = criticalDamage;
        MaxHealthPerc = maxHealthPerc;
        BaseDamagePerc = baseDamagePerc;
        AttackSpeedPerc = attackSpeedPerc;
        DefensePerc = defensePerc;
    }

    public bool IsInRange(Vector3 origin, Vector3 target)
    {
        Vector3 offset = target - origin;
        offset.y = 0f;
        return Range > 0f && offset.sqrMagnitude <= Range * Range;
    }

    public float GetDamage(bool isCritical)
    {
        return isCritical ? Damage * CriticalDamage : Damage;
    }

    public float MitigateDamage(float incomingDamage)
    {
        return Mathf.Max(0f, incomingDamage - EffectiveDefense);
    }

    public Stats Add(Stats other)
    {
        return new Stats(
            MaxHealth + other.MaxHealth,
            BaseDamage + other.BaseDamage,
            AttackSpeed + other.AttackSpeed,
            Defense + other.Defense,
            CriticalChance + other.CriticalChance,
            CriticalDamage + other.CriticalDamage,
            MaxHealthPerc + other.MaxHealthPerc,
            BaseDamagePerc + other.BaseDamagePerc,
            AttackSpeedPerc + other.AttackSpeedPerc,
            DefensePerc + other.DefensePerc,
            MovementSpeed + other.MovementSpeed,
            MultishotChance + other.MultishotChance,
            Range + other.Range
        );
    }
}