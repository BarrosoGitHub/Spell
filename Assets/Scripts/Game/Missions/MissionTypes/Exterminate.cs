using UnityEngine;
using UnityEngine.AI;

public class Exterminate : Mission
{
    private const int EnemyCount = 5;
    private const int SpawnColumns = 10;
    private const float SpawnSpacing = 2f;

    private EnemyGroup enemyGroup;

    public override void StartMission()
    {
        if (State != ReadyState) return;
        base.StartMission();
        if (State != RunningState) return;

        enemyManager.DespawnAllEnemiesAndStopCoroutines();
        if (!NavMesh.SamplePosition(Vector3.zero, out NavMeshHit spawnCenter, 15f, NavMesh.AllAreas))
        {
            Debug.LogError("Exterminate needs a baked NavMesh within 15 units of world origin.", this);
            FailMission();
            return;
        }

        enemyGroup = new EnemyGroup(
            EnemyType.Normal, 
            GameManager.Instance.playerController,
            3,
            15f,
            1,
            3f
        );
        
        enemyManager.AddEnemyGroup(enemyGroup);

        for (int i = 0; i < EnemyCount; i++)
        {
            float x = (i % SpawnColumns - (SpawnColumns - 1) * 0.5f) * SpawnSpacing;
            float z = (i / SpawnColumns - (SpawnColumns - 1) * 0.5f) * SpawnSpacing;
            Vector3 candidate = spawnCenter.position + new Vector3(x, 0f, z);
            Vector3 position = NavMesh.SamplePosition(candidate, out NavMeshHit hit, SpawnSpacing, NavMesh.AllAreas)
                ? hit.position
                : spawnCenter.position;
            enemyManager.SpawnEnemy(EnemyType.Normal, position, Quaternion.identity);
        }

        Debug.Log("Starting Exterminate mission.");
    }

    public override void CompleteMission(MissionResult result)
    {
        if (State != RunningState) return;
        base.CompleteMission(result);

        Debug.Log("Exterminate mission completed.");
    }

    public override void FailMission()
    {
        if (State != RunningState) return;
        enemyManager.DespawnAllEnemiesAndStopCoroutines();
        base.FailMission();

        Debug.Log("Exterminate mission failed.");
    }

    public override void AbortMission()
    {
        enemyManager.DespawnAllEnemiesAndStopCoroutines();
        base.AbortMission();


        Debug.Log("Exterminate mission aborted.");
    }

    public void OnDestroy()
    {
        if (enemyManager != null)
        {
            enemyManager.DespawnAllEnemiesAndStopCoroutines();
            Destroy(enemyManager);
        }
    }
}
