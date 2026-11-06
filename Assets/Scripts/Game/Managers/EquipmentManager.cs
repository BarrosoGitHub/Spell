using System;
using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    [SerializeField] private EquipmentDropTable dropTable;

    public Equipment GenerateEquipment(EquipmentDrop drop)
    {
        if (drop == null) throw new ArgumentNullException(nameof(drop));
        return drop.Generate();
    }

    public Equipment GetRandomEquipment()
    {
        return GetDropTable().Generate();
    }

    public Equipment GetRandomEquipment(Rarity rarity)
    {
        return GetDropTable().Generate(rarity);
    }

    private EquipmentDropTable GetDropTable()
    {
        if (dropTable == null)
            throw new InvalidOperationException("Assign an EquipmentDropTable in the EquipmentManager Inspector.");
        return dropTable;
    }
}
