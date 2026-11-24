using UnityEngine;

[CreateAssetMenu(fileName = "PersistentStatProfile", menuName = "Controllers/Persistent Stat Profile")]
public sealed class PersistentStatProfile : ScriptableObject
{
    [Header("Value Per Level")]
    [SerializeField] private float maxHealth;
    [SerializeField] private float baseDamage;
    [SerializeField] private float attackSpeed;
    [SerializeField] private float movementSpeed;
    [SerializeField] private float defense;
    [SerializeField] private float criticalChance;
    [SerializeField] private float criticalDamage;
    [SerializeField] private float multishotChance;
    [SerializeField] private float range;

    [Header("Percentage Stats")]
    [SerializeField] private float maxHealthPercentage;
    [SerializeField] private float baseDamagePercentage;
    [SerializeField] private float attackSpeedPercentage;
    [SerializeField] private float defensePercentage;

    public Stats CreateStats(PersistentStatData levels)
    {
        if (levels == null)
            throw new System.ArgumentNullException(nameof(levels));

        return new Stats(
            maxHealth: maxHealth * levels.MaxHealth,
            baseDamage: baseDamage * levels.BaseDamage,
            attackSpeed: attackSpeed * levels.AttackSpeed,
            defense: defense * levels.Defense,
            criticalChance: criticalChance * levels.CriticalChance,
            criticalDamage: criticalDamage * levels.CriticalDamage,
            maxHealthPerc: maxHealthPercentage * levels.MaxHealthPercentage,
            baseDamagePerc: baseDamagePercentage * levels.BaseDamagePercentage,
            attackSpeedPerc: attackSpeedPercentage * levels.AttackSpeedPercentage,
            defensePerc: defensePercentage * levels.DefensePercentage,
            movementSpeed: movementSpeed * levels.MovementSpeed,
            multishotChance: multishotChance * levels.MultishotChance,
            range: range * levels.Range);
    }
}
