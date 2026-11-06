using System.Collections.Generic;

public class WaveSpecification
{
    public int waveNumber;
    public List<EnemyGroup> enemyGroups;
    public Dictionary<EnemyType, int> totalEnemiesPerType; 
    public float waveDuration;
}