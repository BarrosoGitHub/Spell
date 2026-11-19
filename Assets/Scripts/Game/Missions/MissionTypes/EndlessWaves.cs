using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public sealed class EndlessWaves : Mission
{
    [Header("Waves")]
    [SerializeField, Min(1)] private int enemiesPerWave = 5;
    [SerializeField, Min(0.1f)] private float waveInterval = 10f;
    [SerializeField, Min(0.1f)] private float spawnClearance = 2f;

    public int CurrentWave { get; private set; }
    public int EnemiesInWave { get; private set; }
    public int EnemiesRemaining => livingEnemies.Count;
    public float SurvivalTime { get; private set; }
    protected override float ElapsedTime => SurvivalTime;

    // Only the newest wave controls early advancement; older survivors remain managed by EnemyManager.
    private readonly HashSet<Controller> livingEnemies = new();
    private ControllerSpawner spawner;
    private Controller player;
    private EnemyGroup enemyGroup;
    private Coroutine waves;
    private Camera gameplayCamera;
    private NavMeshTriangulation navigation;
    private readonly Plane[] cameraPlanes = new Plane[6];
    private NavMeshPath spawnPath;
    private Vector3[] spawnPositions;

    protected override void Awake()
    {
        spawnPath = new NavMeshPath();
        ReadyState.OnEnterState += ResetProgress;
        RunningState.OnEnterState += StartWaves;
        RunningState.OnUpdate += UpdateSurvival;
        RunningState.OnExitState += StopWaves;
        base.Awake();
        spawner = GetComponent<ControllerSpawner>();
    }

    private void OnEnable() => spawner.OnControllerDespawned += HandleEnemyDespawned;

    private void OnDisable()
    {
        if (State == RunningState) AbortMission();
        spawner.OnControllerDespawned -= HandleEnemyDespawned;
    }

    public override void StartMission()
    {
        if (!isActiveAndEnabled || State != ReadyState) return;
        player = GameManager.Instance != null ? GameManager.Instance.playerController : null;
        gameplayCamera = Camera.main;
        navigation = NavMesh.CalculateTriangulation();
        if (gameplayCamera == null || navigation.indices.Length < 3)
        {
            Debug.LogWarning("EndlessWaves requires a MainCamera and a baked NavMesh.", this);
            return;
        }
        spawnPositions = new Vector3[Mathf.Max(1, enemiesPerWave) + 1];
        base.StartMission();
    }

    private void ResetProgress()
    {
        CurrentWave = 0;
        EnemiesInWave = 0;
        SurvivalTime = 0f;
    }

    private void StartWaves()
    {
        enemyManager.DespawnAllEnemiesAndStopCoroutines();
        livingEnemies.Clear();
        enemyGroup = new EnemyGroup(EnemyType.Normal, player,
            maxNumberOfEnemies: 3, maxEngagementDistance: 15f,
            maxNumberOfEnemiesThatCanEngageSimultaneously: 1, attackInterval: 1.5f);
        enemyManager.AddEnemyGroup(enemyGroup);
        enemyManager.AddEnemyGroup(new EnemyGroup(EnemyType.Mutated, player,
            maxNumberOfEnemies: 1, maxEngagementDistance: 15f,
            maxNumberOfEnemiesThatCanEngageSimultaneously: 1, attackInterval: 1.5f));
        waves = StartCoroutine(RunWaves());
    }

    private IEnumerator RunWaves()
    {
        var retrySpawn = new WaitForSeconds(1f);
        while (State == RunningState)
        {
            // Prepare the entire batch before activating any enemies.
            if (!TryFindSpawnPositions())
            {
                yield return retrySpawn;
                continue;
            }

            livingEnemies.Clear();
            CurrentWave++;
            EnemiesInWave = spawnPositions.Length;
            for (int i = 0; i < EnemiesInWave; i++)
            {
                EnemyType type = i == EnemiesInWave - 1 ? EnemyType.Mutated : EnemyType.Normal;
                Controller enemy = enemyManager.SpawnEnemy(type,
                    spawnPositions[i], Quaternion.identity, CurrentWave);
                if (enemy == null)
                {
                    Debug.LogError($"EndlessWaves could not spawn a {type} enemy. Check the enemy pool.", this);
                    FailMission();
                    yield break;
                }
                livingEnemies.Add(enemy);
            }

            float elapsed = 0f;
            while (livingEnemies.Count > 0 && elapsed < waveInterval)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }
        }
    }

    private bool TryFindSpawnPositions()
    {
        if (gameplayCamera == null || player == null) return false;
        GeometryUtility.CalculateFrustumPlanes(gameplayCamera, cameraPlanes);
        if (!NavMesh.SamplePosition(player.transform.position, out NavMeshHit target, 3f, NavMesh.AllAreas))
            return false;

        for (int i = 0; i < spawnPositions.Length; i++)
        {
            float closestDistanceSquared = float.PositiveInfinity;
            for (int attempt = 0; attempt < 256; attempt++)
            {
                int triangle = Random.Range(0, navigation.indices.Length / 3) * 3;
                Vector3 a = navigation.vertices[navigation.indices[triangle]];
                Vector3 b = navigation.vertices[navigation.indices[triangle + 1]];
                Vector3 c = navigation.vertices[navigation.indices[triangle + 2]];
                float u = Mathf.Sqrt(Random.value);
                Vector3 position = (1f - u) * a + u * (1f - Random.value) * (b - c) + u * c;
                if (!NavMesh.SamplePosition(position, out NavMeshHit hit, 1f, NavMesh.AllAreas)) continue;
                position = hit.position;
                float distanceSquared = (position - player.transform.position).sqrMagnitude;
                if (distanceSquared >= closestDistanceSquared) continue;
                Bounds bounds = new Bounds(position + Vector3.up, Vector3.one * spawnClearance * 2f);
                if (GeometryUtility.TestPlanesAABB(cameraPlanes, bounds)) continue;
                bool overlaps = false;
                for (int j = 0; j < i; j++)
                    if ((spawnPositions[j] - position).sqrMagnitude < spawnClearance * spawnClearance)
                        overlaps = true;
                if (overlaps) continue;
                if (!NavMesh.CalculatePath(position, target.position, NavMesh.AllAreas, spawnPath)
                    || spawnPath.status != NavMeshPathStatus.PathComplete) continue;
                spawnPositions[i] = position;
                closestDistanceSquared = distanceSquared;
            }
            if (float.IsPositiveInfinity(closestDistanceSquared)) return false;
        }
        return true;
    }

    private void HandleEnemyDespawned(Controller enemy) => livingEnemies.Remove(enemy);

    private void UpdateSurvival()
    {
        if (player == null || !player.isActiveAndEnabled || player.entity.CurrentHealth <= 0f)
        {
            FailMission();
            return;
        }
        SurvivalTime += Time.deltaTime;
    }

    private void StopWaves()
    {
        if (waves != null) StopCoroutine(waves);
        waves = null;
        enemyManager.DespawnAllEnemiesAndStopCoroutines();
        livingEnemies.Clear();
        enemyGroup = null;
    }
}
