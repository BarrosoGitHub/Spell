using UnityEngine;

public class Exterminate : Mission
{
    private EnemyGroup enemyGroup;

    public override void StartMission()
    {
        if (State != ReadyState) return;
        base.StartMission();
        if (State != RunningState) return;

        enemyManager.DespawnAllEnemiesAndStopCoroutines();
        enemyGroup = new EnemyGroup(
            EnemyType.Normal, 
            GameManager.Instance.playerController,
            3,
            5f,
            1,
            1.5f
        );
        
        enemyManager.AddEnemyGroup(enemyGroup);

        for (int i = 0; i < 10; i++)
        {
            enemyManager.SpawnEnemy(enemyGroup);
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
