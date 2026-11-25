using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;

public sealed class CloudSavePersistentStatStorage : PersistentStatStorage
{
    private const string DataKey = "persistentStats";

    public override async Task<PersistentStatData> LoadAsync(string playerId)
    {
        ValidatePlayer(playerId);

        var items = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { DataKey });
        if (!items.TryGetValue(DataKey, out var item))
            return new PersistentStatData();

        // JsonUtility preserves the data class's private serialized fields.
        string json = item.Value.GetAs<string>();
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("Cloud Save persistent stats contain empty data.");

        PersistentStatData data = JsonUtility.FromJson<PersistentStatData>(json);
        if (data == null)
            throw new InvalidOperationException("Cloud Save persistent stats could not be deserialized.");

        return data;
    }

    public override async Task SaveAsync(string playerId, PersistentStatData data)
    {
        ValidatePlayer(playerId);
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        var items = new Dictionary<string, object>
        {
            { DataKey, JsonUtility.ToJson(data) }
        };
        await CloudSaveService.Instance.Data.Player.SaveAsync(items);
    }

    private static void ValidatePlayer(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            throw new ArgumentException("A player ID is required.", nameof(playerId));
        if (UnityServices.State != ServicesInitializationState.Initialized ||
            !AuthenticationService.Instance.IsSignedIn)
            throw new InvalidOperationException("Sign in before accessing Cloud Save persistent stats.");
        if (!string.Equals(playerId, AuthenticationService.Instance.PlayerId, StringComparison.Ordinal))
            throw new ArgumentException("The player ID must match the authenticated player.", nameof(playerId));
    }
}
