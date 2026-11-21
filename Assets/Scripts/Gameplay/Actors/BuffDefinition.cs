using UnityEngine;

public enum BuffType
{
    Buff,
    Debuff
}

[CreateAssetMenu(fileName = "Buff", menuName = "Gameplay/Buff")]
public sealed class BuffDefinition : ScriptableObject
{
    [SerializeField] private BuffType type;
    [Tooltip("Lifetime in scaled seconds. Zero expires immediately.")]
    [SerializeField, Min(0f)] private float duration = 5f;

    [SerializeField] private float maxHealth;
    [SerializeField] private float baseDamage;
    [SerializeField] private float attackSpeed;
    [SerializeField] private float movementSpeed;
    [SerializeField] private float defense;
    [Tooltip("Percentage points: 5 adds 5% critical chance.")]
    [SerializeField] private float criticalChance;
    [Tooltip("Added multiplier: 0.5 adds 0.5x critical damage.")]
    [SerializeField] private float criticalDamage;

    [Tooltip("Percentage points: 25 adds 25% extra-shot chance.")]
    [SerializeField] private float multishotChance;

    public BuffType Type => type;
    public float Duration => duration;

    public Stats GetStats()
    {
        return new Stats(
            maxHealth: maxHealth,
            baseDamage: baseDamage,
            attackSpeed: attackSpeed,
            movementSpeed: movementSpeed,
            defense: defense,
            criticalChance: criticalChance,
            criticalDamage: criticalDamage,
            multishotChance: multishotChance);
    }
}
