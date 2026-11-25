using System;
using UnityEngine;

public enum PersistentStatType
{
    MaxHealth,
    BaseDamage,
    AttackSpeed,
    MovementSpeed,
    Defense,
    CriticalChance,
    CriticalDamage,
    MultishotChance,
    Range,
    MaxHealthPercentage,
    BaseDamagePercentage,
    AttackSpeedPercentage,
    DefensePercentage
}

[Serializable]
public sealed class PersistentStatData
{
    [SerializeField] private int maxHealth;
    [SerializeField] private int baseDamage;
    [SerializeField] private int attackSpeed;
    [SerializeField] private int movementSpeed;
    [SerializeField] private int defense;
    [SerializeField] private int criticalChance;
    [SerializeField] private int criticalDamage;
    [SerializeField] private int multishotChance;
    [SerializeField] private int range;
    [SerializeField] private int maxHealthPercentage;
    [SerializeField] private int baseDamagePercentage;
    [SerializeField] private int attackSpeedPercentage;
    [SerializeField] private int defensePercentage;

    public int MaxHealth => maxHealth;
    public int BaseDamage => baseDamage;
    public int AttackSpeed => attackSpeed;
    public int MovementSpeed => movementSpeed;
    public int Defense => defense;
    public int CriticalChance => criticalChance;
    public int CriticalDamage => criticalDamage;
    public int MultishotChance => multishotChance;
    public int Range => range;
    public int MaxHealthPercentage => maxHealthPercentage;
    public int BaseDamagePercentage => baseDamagePercentage;
    public int AttackSpeedPercentage => attackSpeedPercentage;
    public int DefensePercentage => defensePercentage;

    public PersistentStatData WithLevel(PersistentStatType stat, int level)
    {
        if (level < 0)
            throw new ArgumentOutOfRangeException(nameof(level));

        PersistentStatData copy = (PersistentStatData)MemberwiseClone();
        switch (stat)
        {
            case PersistentStatType.MaxHealth: copy.maxHealth = level; break;
            case PersistentStatType.BaseDamage: copy.baseDamage = level; break;
            case PersistentStatType.AttackSpeed: copy.attackSpeed = level; break;
            case PersistentStatType.MovementSpeed: copy.movementSpeed = level; break;
            case PersistentStatType.Defense: copy.defense = level; break;
            case PersistentStatType.CriticalChance: copy.criticalChance = level; break;
            case PersistentStatType.CriticalDamage: copy.criticalDamage = level; break;
            case PersistentStatType.MultishotChance: copy.multishotChance = level; break;
            case PersistentStatType.Range: copy.range = level; break;
            case PersistentStatType.MaxHealthPercentage: copy.maxHealthPercentage = level; break;
            case PersistentStatType.BaseDamagePercentage: copy.baseDamagePercentage = level; break;
            case PersistentStatType.AttackSpeedPercentage: copy.attackSpeedPercentage = level; break;
            case PersistentStatType.DefensePercentage: copy.defensePercentage = level; break;
            default: throw new ArgumentOutOfRangeException(nameof(stat), stat, null);
        }

        return copy;
    }
}
