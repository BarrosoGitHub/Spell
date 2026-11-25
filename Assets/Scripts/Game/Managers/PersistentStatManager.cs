using System;
using System.Threading.Tasks;
using UnityEngine;

public sealed class PersistentStatManager : MonoBehaviour
{
    [SerializeField] private PersistentStatProfile profile;
    [SerializeField] private PersistentStatStorage storage;

    private string playerId;
    private Entity entity;
    private PersistentStat modifier;

    public PersistentStatData Levels { get; private set; }
    public bool IsLoaded => Levels != null;

    public event Action<PersistentStatData> OnLevelsChanged;

    public async Task LoadAsync(string id, Entity target)
    {
        if (profile == null || storage == null)
            throw new InvalidOperationException("Assign a profile and storage provider.");
        if (target == null)
            throw new ArgumentNullException(nameof(target));

        PersistentStatData loadedLevels = await storage.LoadAsync(id);
        if (this == null || target == null)
            return;
        playerId = id;
        ApplyLevels(loadedLevels ?? new PersistentStatData(), target);
    }

    public async Task SetLevelAsync(PersistentStatType stat, int level)
    {
        if (!IsLoaded)
            throw new InvalidOperationException("Load the player's persistent stats first.");

        PersistentStatData updatedLevels = Levels.WithLevel(stat, level);
        await storage.SaveAsync(playerId, updatedLevels);

        if (this == null || entity == null)
            return;
        ApplyLevels(updatedLevels, entity);
    }

    public async Task ResetAsync()
    {
        if (!IsLoaded)
            throw new InvalidOperationException("Load the player's persistent stats first.");

        PersistentStatData resetLevels = new PersistentStatData();
        await storage.SaveAsync(playerId, resetLevels);
        if (this == null || entity == null)
            return;
        ApplyLevels(resetLevels, entity);
    }

    private void ApplyLevels(PersistentStatData levels, Entity target)
    {
        RemoveModifier();
        entity = target;
        Levels = levels;
        modifier = new PersistentStat(profile, Levels);
        entity.AddStatModifier(modifier);
        OnLevelsChanged?.Invoke(Levels);
    }

    private void RemoveModifier()
    {
        if (entity != null && modifier != null)
            entity.RemoveStatModifier(modifier);
        modifier = null;
    }

    private void OnDestroy()
    {
        RemoveModifier();
    }
}
