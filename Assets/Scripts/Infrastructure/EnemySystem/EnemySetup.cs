using System;
using System.Collections.Generic;
using UnityEngine;

// Registers engagement groups and spawns an initial batch, independently of waves.
[RequireComponent(typeof(EnemyManager))]
public class EnemySetup : MonoBehaviour
{
    [Serializable]
    public class GroupSettings
    {
        public EnemyType enemyType = EnemyType.Normal;
        [Min(0)] public int spawnCount = 5;
        [Min(1)] public int maxFollowingEnemies = 3;
        [Min(0.1f)] public float engagementDistance = 15f;
        [Tooltip("Maximum distance to the target for each enemy to begin an attack, in world units.")]
        [Min(0f)] public float attackRange = 3f;
        [Min(1)] public int attackersPerCycle = 1;
        [Min(0.1f)] public float attackInterval = 3f;
    }

    [SerializeField] private Controller target;
    [Tooltip("Place these on the baked NavMesh. Enemies use them in order, wrapping if needed.")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private GroupSettings[] groups = { new GroupSettings() };

    private EnemyManager enemyManager;
    private readonly List<EnemyGroup> registeredGroups = new();

    private void Start()
    {
        enemyManager = GetComponent<EnemyManager>();
        if (!target || spawnPoints == null || spawnPoints.Length == 0 ||
            Array.Exists(spawnPoints, point => !point))
        {
            
            return;
        }

        if (groups == null) return;
        var registeredTypes = new HashSet<EnemyType>();
        int spawnIndex = 0;
        foreach (GroupSettings settings in groups)
        {
            // One group per type avoids competing for the same enemies and target.
            if (settings == null || !registeredTypes.Add(settings.enemyType)) continue;

            var group = new EnemyGroup(settings.enemyType, target,
                Mathf.Max(1, settings.maxFollowingEnemies),
                Mathf.Max(0.1f, settings.engagementDistance),
                Mathf.Max(1, settings.attackersPerCycle),
                Mathf.Max(0.1f, settings.attackInterval),
                Mathf.Max(0f, settings.attackRange));
            enemyManager.AddEnemyGroup(group);
            registeredGroups.Add(group);

            for (int i = 0; i < settings.spawnCount; i++)
            {
                Transform point = spawnPoints[spawnIndex++ % spawnPoints.Length];
                enemyManager.SpawnEnemy(settings.enemyType, point.position, point.rotation);
            }
        }
    }

    private void OnDestroy()
    {
        if (!enemyManager) return;
        foreach (EnemyGroup group in registeredGroups)
            enemyManager.RemoveEnemyGroup(group);
    }
}
