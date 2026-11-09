using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class EndlessWaves : Mission
{
    [Header("Waves")]
    [SerializeField, Min(1)] private int firstWaveEnemies = 3;
    [SerializeField, Min(1)] private int enemiesAddedPerWave = 2;
    [SerializeField, Min(0.1f)] private float spawnInterval = 0.2f;
    [SerializeField, Min(0f)] private float waveBreak = 1f;

    public int CurrentWave { get; private set; }
    public int EnemiesInWave { get; private set; }
    public int EnemiesRemaining => livingEnemies.Count;
    public float SurvivalTime { get; private set; }
    protected override float ElapsedTime => SurvivalTime;

    // Group membership changes with distance, so track this mission's spawns separately.
    private readonly HashSet<Controller> livingEnemies = new();
    private ControllerSpawner spawner;
    private Controller player;
    private EnemyGroup enemyGroup;
    private Coroutine waves;

    protected override void Awake()
    {
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
        if (player == null || player.entity == null || player.entity.CurrentHealth <= 0f)
        {
            Debug.LogWarning("EndlessWaves requires a living player before starting.", this);
            return;
        }
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
        waves = StartCoroutine(RunWaves());
    }

    private IEnumerator RunWaves()
    {
        var betweenSpawns = new WaitForSeconds(Mathf.Max(0.1f, spawnInterval));
        var betweenWaves = new WaitForSeconds(Mathf.Max(0f, waveBreak));

        while (State == RunningState)
        {
            CurrentWave++;
            EnemiesInWave = Mathf.Max(1, firstWaveEnemies)
                + (CurrentWave - 1) * Mathf.Max(1, enemiesAddedPerWave);

            for (int i = 0; i < EnemiesInWave; i++)
            {
                Controller enemy = enemyManager.SpawnEnemy(enemyGroup);
                if (enemy == null)
                {
                    Debug.LogError("EndlessWaves could not spawn a Normal enemy. Check the enemy pool.", this);
                    FailMission();
                    yield break;
                }
                livingEnemies.Add(enemy);
                if (i < EnemiesInWave - 1) yield return betweenSpawns;
            }

            // Start the break only after the entire wave has spawned and been cleared.
            while (livingEnemies.Count > 0) yield return null;
            yield return betweenWaves;
        }
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
