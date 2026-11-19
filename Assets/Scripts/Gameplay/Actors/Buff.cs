public sealed class Buff : StatModifier
{
    public BuffDefinition Definition { get; }
    public BuffType Type => Definition.Type;
    public float Duration => Definition.Duration;

    public Buff(BuffDefinition definition)
    {
        if (definition == null)
            throw new System.ArgumentNullException(nameof(definition));

        Definition = definition;
        Name = definition.name;
        Stats = definition.GetStats();
    }
}
