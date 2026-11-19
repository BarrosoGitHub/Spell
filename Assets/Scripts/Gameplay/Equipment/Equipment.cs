public class Equipment : StatModifier
{
    public SlotType Type { get; set; }
    public Rarity Rarity { get; set; }
}

public enum SlotType
{
    Rune = 0,
}

public enum Rarity
{
    Common = 0,
    Uncommon = 1,
    Rare = 2,
    Epic = 3,
    Legendary = 4,
}
