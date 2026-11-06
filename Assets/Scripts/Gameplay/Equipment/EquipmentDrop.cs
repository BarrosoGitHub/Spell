using UnityEngine;

[CreateAssetMenu(fileName = "EquipmentDrop", menuName = "Equipment/Drop")]
public class EquipmentDrop : ScriptableObject
{
    [SerializeField] private string equipmentName;
    [SerializeField] private SlotType slot;
    [SerializeField] private Rarity rarity;
    [SerializeField] private EquipmentRaritySettings raritySettings;

    private enum StatLine
    {
        MaxHealth, BaseDamage, AttackSpeed, Defense, CriticalChance, CriticalDamage,
        MaxHealthPerc, BaseDamagePerc, AttackSpeedPerc, DefensePerc, Count
    }

    [Header("Stat Ranges (Min, Max)")]
    [SerializeField] private Vector2 maxHealth;
    [SerializeField] private Vector2 baseDamage;
    [SerializeField] private Vector2 attackSpeed;
    [SerializeField] private Vector2 defense;
    [Tooltip("Percentage points: 5 means +5% critical chance.")]
    [SerializeField] private Vector2 criticalChance;
    [Tooltip("Added to the critical damage multiplier: 0.5 means +0.5x.")]
    [SerializeField] private Vector2 criticalDamage;

    [Header("Percentage Bonus Ranges (0.05 = 5%)")]
    [SerializeField] private Vector2 maxHealthPerc;
    [SerializeField] private Vector2 baseDamagePerc;
    [SerializeField] private Vector2 attackSpeedPerc;
    [SerializeField] private Vector2 defensePerc;

    public Rarity Rarity => rarity;

    public Equipment Generate()
    {
        if (raritySettings == null)
            throw new System.InvalidOperationException("Assign EquipmentRaritySettings in the EquipmentDrop Inspector.");

        int selectedLines = SelectLines(raritySettings.GetMaxLines(rarity));
        return new Equipment
        {
            Name = equipmentName,
            Type = slot,
            Rarity = rarity,
            Stats = new Stats(
                maxHealth: Roll(selectedLines, StatLine.MaxHealth),
                baseDamage: Roll(selectedLines, StatLine.BaseDamage),
                attackSpeed: Roll(selectedLines, StatLine.AttackSpeed),
                defense: Roll(selectedLines, StatLine.Defense),
                criticalChance: Roll(selectedLines, StatLine.CriticalChance),
                criticalDamage: Roll(selectedLines, StatLine.CriticalDamage),
                maxHealthPerc: Roll(selectedLines, StatLine.MaxHealthPerc),
                baseDamagePerc: Roll(selectedLines, StatLine.BaseDamagePerc),
                attackSpeedPerc: Roll(selectedLines, StatLine.AttackSpeedPerc),
                defensePerc: Roll(selectedLines, StatLine.DefensePerc))
        };
    }

    private int SelectLines(int maximum)
    {
        int remainingCandidates = 0;
        for (int i = 0; i < (int)StatLine.Count; i++)
        {
            Vector2 range = GetRange((StatLine)i);
            if (range.x != 0f || range.y != 0f) remainingCandidates++;
        }

        int remainingLines = Mathf.Min(maximum, remainingCandidates);
        int selectedLines = 0;
        // Select uniformly without replacement; a bit per stat avoids temporary collections.
        for (int i = 0; i < (int)StatLine.Count && remainingLines > 0; i++)
        {
            Vector2 range = GetRange((StatLine)i);
            if (range.x == 0f && range.y == 0f) continue;
            if (Random.Range(0, remainingCandidates) < remainingLines)
            {
                selectedLines |= 1 << i;
                remainingLines--;
            }
            remainingCandidates--;
        }
        return selectedLines;
    }

    private float Roll(int selectedLines, StatLine stat)
    {
        if ((selectedLines & (1 << (int)stat)) == 0) return 0f;
        Vector2 range = GetRange(stat);
        return Random.Range(Mathf.Min(range.x, range.y), Mathf.Max(range.x, range.y));
    }

    private Vector2 GetRange(StatLine stat)
    {
        return stat switch
        {
            StatLine.MaxHealth => maxHealth,
            StatLine.BaseDamage => baseDamage,
            StatLine.AttackSpeed => attackSpeed,
            StatLine.Defense => defense,
            StatLine.CriticalChance => criticalChance,
            StatLine.CriticalDamage => criticalDamage,
            StatLine.MaxHealthPerc => maxHealthPerc,
            StatLine.BaseDamagePerc => baseDamagePerc,
            StatLine.AttackSpeedPerc => attackSpeedPerc,
            StatLine.DefensePerc => defensePerc,
            _ => throw new System.ArgumentOutOfRangeException(nameof(stat))
        };
    }
}
