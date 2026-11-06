using System;
using UnityEngine;

[CreateAssetMenu(fileName = "EquipmentDropTable", menuName = "Equipment/Drop Table")]
public class EquipmentDropTable : ScriptableObject
{
    [Tooltip("Relative chances: weights of 70, 20, and 10 give 70%, 20%, and 10%. Zero disables an entry.")]
    [SerializeField] private Entry[] entries = new Entry[0];

    [Serializable]
    public class Entry
    {
        public EquipmentDrop drop;
        [Min(0f)] public float weight = 1f;
    }

    public Equipment Generate()
    {
        return GenerateMatching(null);
    }

    public Equipment Generate(Rarity rarity)
    {
        return GenerateMatching(rarity);
    }

    private Equipment GenerateMatching(Rarity? rarity)
    {
        if (entries == null || entries.Length == 0)
            throw new InvalidOperationException("EquipmentDropTable requires at least one entry.");

        float totalWeight = 0f;
        EquipmentDrop lastEnabledDrop = null;
        foreach (Entry entry in entries)
        {
            if (entry == null || entry.weight < 0f || float.IsNaN(entry.weight) || float.IsInfinity(entry.weight))
                throw new InvalidOperationException("EquipmentDropTable entries require finite, nonnegative weights.");

            if (entry.weight == 0f) continue;
            if (entry.drop == null)
                throw new InvalidOperationException("Assign an EquipmentDrop to each enabled EquipmentDropTable entry.");

            if (rarity.HasValue && entry.drop.Rarity != rarity.Value) continue;

            totalWeight += entry.weight;
            lastEnabledDrop = entry.drop;
        }

        if (lastEnabledDrop == null || float.IsInfinity(totalWeight))
            throw new InvalidOperationException("EquipmentDropTable requires a finite positive total weight for the requested rarity or pool.");

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        foreach (Entry entry in entries)
        {
            if (entry.weight == 0f || (rarity.HasValue && entry.drop.Rarity != rarity.Value)) continue;
            if (roll < entry.weight) return entry.drop.Generate();
            roll -= entry.weight;
        }

        // Unity's float Random.Range includes its upper bound.
        return lastEnabledDrop.Generate();
    }
}
