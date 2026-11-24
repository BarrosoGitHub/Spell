public sealed class PersistentStat : StatModifier
{
    public PersistentStat(PersistentStatProfile profile, PersistentStatData levels)
    {
        if (profile == null)
            throw new System.ArgumentNullException(nameof(profile));
        if (levels == null)
            throw new System.ArgumentNullException(nameof(levels));

        Name = profile.name;
        Stats = profile.CreateStats(levels);
    }
}
