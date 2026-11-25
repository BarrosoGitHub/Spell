using System;
using System.Threading.Tasks;
using UnityEngine;

public sealed class PersistentStatLevelsDebugModule
{
    private readonly PersistentStatType[] stats = (PersistentStatType[])Enum.GetValues(typeof(PersistentStatType));
    private readonly string[] names;
    private readonly string[] levelLabels;
    private PersistentStatData displayedLevels;
    private bool pending;
    private string status = string.Empty;
    private Vector2 scroll;

    public PersistentStatLevelsDebugModule()
    {
        names = new string[stats.Length];
        levelLabels = new string[stats.Length];
        for (int i = 0; i < stats.Length; i++)
            names[i] = stats[i].ToString();
    }

    public void Draw(PersistentStatManager manager, PlayerAuthentication authentication, Entity target)
    {
        GUILayout.BeginArea(new Rect(16f, 16f, 370f, Mathf.Max(100f, Mathf.Min(530f, Screen.height - 32f))), GUI.skin.box);
        GUILayout.Label("PERSISTENT STAT LEVELS — DEBUG");
        if (manager == null)
        {
            GUILayout.Label("Assign Persistent Stat Manager on DebugManager.");
            GUILayout.EndArea();
            return;
        }

        bool previousEnabled = GUI.enabled;
        GUI.enabled = previousEnabled && !pending;
        if (!manager.IsLoaded)
        {
            GUI.enabled = GUI.enabled && authentication != null && target != null;
            if (GUILayout.Button("Load player levels"))
                RunAsync(() => LoadAsync(manager, authentication, target));
        }
        else
        {
            PersistentStatData levels = manager.Levels;
            if (!ReferenceEquals(displayedLevels, levels))
            {
                displayedLevels = levels;
                for (int i = 0; i < stats.Length; i++)
                    levelLabels[i] = GetLevel(levels, stats[i]).ToString();
            }

            scroll = GUILayout.BeginScrollView(scroll);
            for (int i = 0; i < stats.Length; i++)
            {
                PersistentStatType stat = stats[i];
                int level = GetLevel(levels, stat);
                GUILayout.BeginHorizontal();
                GUILayout.Label(names[i], GUILayout.Width(205f));
                GUI.enabled = previousEnabled && !pending && level > 0;
                if (GUILayout.Button("−", GUILayout.Width(30f)))
                    RunAsync(() => manager.SetLevelAsync(stat, level - 1));
                GUILayout.Label(levelLabels[i], GUILayout.Width(50f));
                GUI.enabled = previousEnabled && !pending && level < int.MaxValue;
                if (GUILayout.Button("+", GUILayout.Width(30f)))
                    RunAsync(() => manager.SetLevelAsync(stat, level + 1));
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUI.enabled = previousEnabled && !pending;
            if (GUILayout.Button("Reset all levels to zero"))
                RunAsync(manager.ResetAsync);
        }

        GUI.enabled = previousEnabled;
        GUILayout.Label(pending ? "Saving / loading…" : status);
        GUILayout.EndArea();
    }

    // The UI owns error reporting; storage errors must not escape an async button handler.
    private async void RunAsync(Func<Task> operation)
    {
        if (pending) return;
        pending = true;
        status = string.Empty;
        try
        {
            await operation();
            status = "Done.";
        }
        catch (Exception exception)
        {
            status = "Failed. Check the Console, then retry.";
            Debug.LogException(exception);
        }
        finally
        {
            pending = false;
        }
    }

    private static async Task LoadAsync(PersistentStatManager manager, PlayerAuthentication authentication, Entity target)
    {
        await authentication.SignInAsync();
        if (manager == null || target == null)
            return;
        await manager.LoadAsync(authentication.PlayerId, target);
    }

    private static int GetLevel(PersistentStatData levels, PersistentStatType stat)
    {
        switch (stat)
        {
            case PersistentStatType.MaxHealth: return levels.MaxHealth;
            case PersistentStatType.BaseDamage: return levels.BaseDamage;
            case PersistentStatType.AttackSpeed: return levels.AttackSpeed;
            case PersistentStatType.MovementSpeed: return levels.MovementSpeed;
            case PersistentStatType.Defense: return levels.Defense;
            case PersistentStatType.CriticalChance: return levels.CriticalChance;
            case PersistentStatType.CriticalDamage: return levels.CriticalDamage;
            case PersistentStatType.MultishotChance: return levels.MultishotChance;
            case PersistentStatType.Range: return levels.Range;
            case PersistentStatType.MaxHealthPercentage: return levels.MaxHealthPercentage;
            case PersistentStatType.BaseDamagePercentage: return levels.BaseDamagePercentage;
            case PersistentStatType.AttackSpeedPercentage: return levels.AttackSpeedPercentage;
            case PersistentStatType.DefensePercentage: return levels.DefensePercentage;
            default: throw new ArgumentOutOfRangeException(nameof(stat), stat, null);
        }
    }
}
