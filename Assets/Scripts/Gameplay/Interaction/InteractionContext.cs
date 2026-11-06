public enum InteractionType
{
    Attack,
    Heal
}

public class InteractionContext
{
    public InteractionType Type { get; set; }
    public float Value { get; set; }
    public bool IsCritical { get; set; }
    public float? DamageOverride { get; set; }
}
