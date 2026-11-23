using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public sealed class MobTest : Mission
{
    [SerializeField] private EnemyType mobType = EnemyType.Normal;
    [SerializeField, Min(1)] private int level = 1;
    [SerializeField, Min(1)] private int summonCount = 1;
    private readonly List<Vector3> batchPositions = new();
    [Tooltip("Distance from the player in world units.")]
    [SerializeField, Min(2f)] private float spawnDistance = 5f;
    [Tooltip("Combat settings for each test mob type. Spawn counts are unused.")]
    [SerializeField] private EnemySetup.GroupSettings[] combatGroups =
    {
        new() { enemyType = EnemyType.Normal, maxFollowingEnemies = 100 },
        new() { enemyType = EnemyType.Mutated, maxFollowingEnemies = 100, attackRange = 7f },
        new() { enemyType = EnemyType.Boss, maxFollowingEnemies = 100, attackRange = 12f },
        new() { enemyType = EnemyType.Tank, maxFollowingEnemies = 100 }
    };

    private readonly HashSet<EnemyType> registeredTypes = new();
    private Controller player;
    private NavMeshPath spawnPath;

    protected override void Awake()
    {
        spawnPath = new NavMeshPath();
        RunningState.OnExitState += ClearMobs;
        base.Awake();
    }

    public override void StartMission()
    {
        if (State != ReadyState) return;
        player = GameManager.Instance != null ? GameManager.Instance.playerController : null;
        if (player == null)
        {
            Debug.LogWarning("MobTest requires a player before starting.", this);
            return;
        }
        base.StartMission();
    }

    public void SpawnMob()
    {
        if (!Application.isPlaying || !isActiveAndEnabled || State != RunningState) return;
        if (player == null || !player.isActiveAndEnabled || player.entity.CurrentHealth <= 0f) return;
        EnemySetup.GroupSettings settings = FindCombatSettings();
        if (settings == null)
        {
            Debug.LogWarning($"Add combat settings for {mobType} in MobTest's Inspector.", this);
            return;
        }
        if (registeredTypes.Add(mobType))
            enemyManager.AddEnemyGroup(new EnemyGroup(mobType, player,
                Mathf.Max(1, settings.maxFollowingEnemies), Mathf.Max(1f, settings.engagementDistance),
                Mathf.Max(1, settings.attackersPerCycle), Mathf.Max(0.1f, settings.attackInterval),
                Mathf.Max(0f, settings.attackRange)));

        batchPositions.Clear();
        int count = Mathf.Max(1, summonCount);
        for (int i = 0; i < count; i++)
        {
            if (!TryFindSpawnPosition(i, count, out Vector3 position))
            {
                Debug.LogWarning($"MobTest spawned {i} of {count} mobs: no more spaced, reachable positions near the player.", this);
                break;
            }
            Vector3 facing = player.transform.position - position;
            facing.y = 0f;
            EnemyController enemy = enemyManager.SpawnEnemy(mobType, position,
                facing.sqrMagnitude > 0.001f ? Quaternion.LookRotation(facing) : Quaternion.identity,
                Mathf.Max(1, level));
            if (enemy == null)
            {
                Debug.LogWarning($"MobTest could not spawn {mobType}. Register its prefab in the enemy pool.", this);
                break;
            }
            batchPositions.Add(position);
        }
        batchPositions.Clear();
    }

    private EnemySetup.GroupSettings FindCombatSettings()
    {
        if (combatGroups == null) return null;
        for (int i = 0; i < combatGroups.Length; i++)
            if (combatGroups[i] != null && combatGroups[i].enemyType == mobType)
                return combatGroups[i];
        return null;
    }

    private bool TryFindSpawnPosition(int spawnIndex, int count, out Vector3 position)
    {
        position = default;
        if (!NavMesh.SamplePosition(player.transform.position, out NavMeshHit origin, 3f, NavMesh.AllAreas))
            return false;
        Vector3 forward = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized;
        for (int i = 0; i < 12; i++)
        {
            float angle = spawnIndex * 360f / count + i * 30f;
            Vector3 candidate = origin.position + Quaternion.Euler(0f, angle, 0f) * forward * Mathf.Max(2f, spawnDistance);
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 1f, NavMesh.AllAreas) ||
                (hit.position - origin.position).sqrMagnitude < 4f) continue;
            bool overlaps = false;
            float spacing = mobType == EnemyType.Boss ? 3f : 1.5f;
            for (int j = 0; j < batchPositions.Count; j++)
                if ((batchPositions[j] - hit.position).sqrMagnitude < spacing * spacing)
                    overlaps = true;
            if (overlaps) continue;
            if (!NavMesh.CalculatePath(hit.position, origin.position, NavMesh.AllAreas, spawnPath) ||
                spawnPath.status != NavMeshPathStatus.PathComplete) continue;
            position = hit.position;
            return true;
        }
        return false;
    }

    public void ClearMobs()
    {
        if (enemyManager != null) enemyManager.DespawnAllEnemiesAndStopCoroutines();
        registeredTypes.Clear();
    }

    private void OnDisable()
    {
        if (State == RunningState) AbortMission();
        else ClearMobs();
    }
}
