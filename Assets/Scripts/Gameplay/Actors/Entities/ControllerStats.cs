using UnityEngine;

[CreateAssetMenu(fileName = "ControllerStats", menuName = "Controllers/Stats")]
public sealed class StatProfile : ScriptableObject
{
    [Header("Initial Base Stats")]
    [SerializeField, Min(1f)] private float initialMaxHealth = 5f;
    [SerializeField, Min(0f)] private float initialDamage = 5f;
    [SerializeField, Min(0.01f)] private float initialAttackSpeed = 1f;
    [SerializeField, Min(0f)] private float initialDefense = 1f;
    [SerializeField, Min(0f)] private float initialMovementSpeed = 2.5f;
    [Tooltip("Percentage points. A value of 5 means 5% critical chance.")]
    [SerializeField, Range(0f, 100f)] private float initialCriticalChance;
    [Tooltip("Critical damage multiplier. A value of 1.5 means 1.5x damage.")]
    [SerializeField, Min(1f)] private float initialCriticalDamage = 1f;

    public Stats CreateBaseStats()
    {
        return new Stats(
            maxHealth: initialMaxHealth,
            baseDamage: initialDamage,
            attackSpeed: initialAttackSpeed,
            defense: initialDefense,
            movementSpeed: initialMovementSpeed,
            criticalChance: initialCriticalChance,
            criticalDamage: initialCriticalDamage);
    }

    [Header("Stat Gains Per Level Above Level 1")]
    [SerializeField, Min(0f)] private float maxHealth = 2f;
    [SerializeField, Min(0f)] private float damage = 0.5f;
    [SerializeField, Min(0f)] private float defense;
    [SerializeField, Min(0f)] private float movementSpeed;
    [Tooltip("Percentage points per level. A value of 1 adds 1% critical chance.")]
    [SerializeField, Min(0f)] private float criticalChance;
    [Tooltip("Added to the critical damage multiplier per level.")]
    [SerializeField, Min(0f)] private float criticalDamage;

    public Stats GetBonus(int level)
    {
        int increases = Mathf.Max(1, level) - 1;
        return new Stats(
            maxHealth: maxHealth * increases,
            baseDamage: damage * increases,
            defense: defense * increases,
            movementSpeed: movementSpeed * increases,
            criticalChance: criticalChance * increases,
            criticalDamage: criticalDamage * increases);
    }
}
