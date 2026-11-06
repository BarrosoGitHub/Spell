using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveSystemManager : MonoBehaviour
{
    public Action<WavePhase> OnWavePhaseChanged;
    public Action<WaveSpecification> OnNewWaveSpecificationGenerated;

    public GameManager gameManager;

    [SerializeField]
    private WavePhase currentWavePhase;
    public WavePhase CurrentWavePhase 
    {
        get { return currentWavePhase; }
        private set
        {
            if (currentWavePhase == value)
                return;

            currentWavePhase = value;
            OnWavePhaseChanged?.Invoke(currentWavePhase);
        }
    }

    [field: SerializeField]
    public int CurrentWave { get; private set; }
    public float currentWaveDuration;

    private void Awake()
    {
        gameManager = GetComponent<GameManager>();
    }

    private IEnumerator StartWavesCoroutine()
    {
        while (true)
        {
            StartNewWave();

            // Wait for the wave to end before starting the next one
            yield return new WaitUntil(() => CurrentWavePhase == WavePhase.Complete);

            // Add a short delay between waves if needed
            yield return new WaitForSeconds(2f);
        }
    }

    private void StartNewWave()
    {
        CurrentWave++;
        GenerateWaveSpecification();
        CurrentWavePhase = WavePhase.WaveInProgress;
        StartCoroutine(CompleteWaveAfterDelayCoroutine());
    }

    private IEnumerator CompleteWaveAfterDelayCoroutine()
    {
        yield return new WaitForSeconds(3);
        CurrentWavePhase = WavePhase.Complete;
    }

    public void GenerateWaveSpecification()
    {
        WaveSpecification waveSpec = new WaveSpecification
        {
            waveNumber = CurrentWave,
            enemyGroups = GenerateEnemyGroupsForWave(CurrentWave),
            totalEnemiesPerType = CalculateTotalEnemiesPerTypeForWave(CurrentWave), // TODO: generate based on round config
            waveDuration = currentWaveDuration
        };

        OnNewWaveSpecificationGenerated?.Invoke(waveSpec);
    }

    private List<EnemyGroup> GenerateEnemyGroupsForWave(int waveNumber)
    {
        Controller targetController = GameManager.Instance.playerController;
        int maxNumberOfEnemies = 3;
        float maxEngagementDistance = 15f;
        int maxNumberOfEnemiesThatCanEngageSimultaneously = 1;
        float attackInterval = Mathf.Max(0.2f, 1f / (1f + 0.3f * Mathf.Log(waveNumber))); // Round 10 0.59s, Round 20 0.53s,    

        List<EnemyGroup> enemyGroups = new List<EnemyGroup>
        {
            new EnemyGroup(EnemyType.Normal, targetController, maxNumberOfEnemies, maxEngagementDistance, maxNumberOfEnemiesThatCanEngageSimultaneously, 3),
            // new EnemyGroup(EnemyType.Tank, targetController, 2, 15, 1, 1),
            // new EnemyGroup(EnemyType.Boss, null, 0, 0f, 0, 0f)
        };

        return enemyGroups;
    }

    private Dictionary<EnemyType, int> CalculateTotalEnemiesPerTypeForWave(int waveNumber)
    {
        Dictionary<EnemyType, int> totals = new Dictionary<EnemyType, int>
        {
            { EnemyType.Normal, (waveNumber) },
            // { EnemyType.Tank, Mathf.Max(0, (waveNumber - 5) / 10) }, // Start spawning tanks at round 5, then add 1 every 10 rounds
            // { EnemyType.Boss, waveNumber >= 5 ? 1 : 0 } // Spawn 1 boss every round starting at round 5
        };

        return totals;
    }

    private void OnGameStateChanged(GameState gameState)
    {
        switch (gameState)
        {
            case GameState.MainMenu:
                CurrentWave = 0;
                break;
            case GameState.Playing:
                StartCoroutine(StartWavesCoroutine());
                break;
            case GameState.Paused:
   
                break;
            default:

                break;
        }
    }

    private void OnEnable()
    {
        gameManager.OnGameStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        gameManager.OnGameStateChanged -= OnGameStateChanged;
    }
}
