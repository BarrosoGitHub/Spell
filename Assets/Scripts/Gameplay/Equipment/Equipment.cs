public class Equipment
{
    public string Name { get; set; }
    public SlotType Type { get; set; }
    public Rarity Rarity { get; set; }
    public Stats Stats { get; set; }
}

public enum SlotType
{
    Boots,
}

public enum Rarity
{
    Common = 0,
    Uncommon = 1,
    Rare = 2,
    Epic = 3,
    Legendary = 4,
}
