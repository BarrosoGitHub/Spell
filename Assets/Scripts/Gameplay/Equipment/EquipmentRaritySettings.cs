using System;
using UnityEngine;

[CreateAssetMenu(fileName = "EquipmentRaritySettings", menuName = "Equipment/Rarity Settings")]
public class EquipmentRaritySettings : ScriptableObject
{
    [Header("Maximum Stat Lines")]
    [SerializeField, Min(0)] private int common = 1;
    [SerializeField, Min(0)] private int uncommon = 2;
    [SerializeField, Min(0)] private int rare = 3;
    [SerializeField, Min(0)] private int epic = 4;
    [SerializeField, Min(0)] private int legendary = 5;

    public int GetMaxLines(Rarity rarity)
    {
        int maximum = rarity switch
        {
            Rarity.Common => common,
            Rarity.Uncommon => uncommon,
            Rarity.Rare => rare,
            Rarity.Epic => epic,
            Rarity.Legendary => legendary,
            _ => throw new ArgumentOutOfRangeException(nameof(rarity))
        };
        return Mathf.Max(0, maximum);
    }
}
