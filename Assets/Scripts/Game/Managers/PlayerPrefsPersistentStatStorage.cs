using System;
using System.Threading.Tasks;
using UnityEngine;

public sealed class PlayerPrefsPersistentStatStorage : PersistentStatStorage
{
    [SerializeField] private string keyPrefix = "PersistentStats.";

    public override Task<PersistentStatData> LoadAsync(string playerId)
    {
        ValidatePlayerId(playerId);
        string json = PlayerPrefs.GetString(keyPrefix + playerId, string.Empty);
        PersistentStatData data = string.IsNullOrEmpty(json)
            ? new PersistentStatData()
            : JsonUtility.FromJson<PersistentStatData>(json);
        return Task.FromResult(data ?? new PersistentStatData());
    }

    public override Task SaveAsync(string playerId, PersistentStatData data)
    {
        ValidatePlayerId(playerId);
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        PlayerPrefs.SetString(keyPrefix + playerId, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
        return Task.CompletedTask;
    }

    private static void ValidatePlayerId(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            throw new ArgumentException("A player ID is required.", nameof(playerId));
    }
}
