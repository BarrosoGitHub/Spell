
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

[RequireComponent(typeof(ControllerSpawner))]
public class EnemyManager : MonoBehaviour
{
    private ControllerSpawner controllerSpawner;
    public float refreshEnemyFollowGroupInterval = 0.25f;
    private readonly Dictionary<EnemyGroup, Coroutine[]> groupCoroutines = new();
    private List<Controller> availableControllers = new();
    private readonly HashSet<EnemyController> spawnedEnemies = new();
    private EntityManager entityManager;

    void Awake()
    {
        controllerSpawner = GetComponent<ControllerSpawner>();
    }

    private void OnEnable()
    {
        entityManager = EntityManager.Instance;
        entityManager.OnControllerDied += HandleControllerDied;
    }

    private void OnDisable()
    {
        if (entityManager != null)
            entityManager.OnControllerDied -= HandleControllerDied;
        entityManager = null;
    }

    private void HandleControllerDied(Controller source, Controller target)
    {
        if (target is EnemyController enemy && spawnedEnemies.Contains(enemy))
            DespawnEnemy(enemy);
    }

    public void AddEnemyGroup(EnemyGroup enemyGroup)
    {
        if (enemyGroup == null || groupCoroutines.ContainsKey(enemyGroup))
            return;

        if (!enemyGroup.TargetController)
        {
            return;
        }

        groupCoroutines[enemyGroup] = new[]
        {
            StartCoroutine(RefreshAttackingEnemyGroupCoroutine(enemyGroup)),
            StartCoroutine(PerformEnemyGroupAttackCoroutine(enemyGroup))
        };
    }

    public bool RemoveEnemyGroup(EnemyGroup enemyGroup)
    {
        if (enemyGroup == null)
            return false;

        if (groupCoroutines.TryGetValue(enemyGroup, out var coroutines))
        {
            foreach (Coroutine coroutine in coroutines)
                if (coroutine != null)
                    StopCoroutine(coroutine);
        }

        foreach (EnemyController enemy in enemyGroup.EnemyList.ToArray())
            RemoveControllerFromGroup(enemyGroup, enemy);

        groupCoroutines.Remove(enemyGroup);
        return true;
    }

    public Controller SpawnEnemy(EnemyGroup enemyGroup)
    {
        if (enemyGroup == null || !enemyGroup.TargetController) return null;

        EnemyController enemy = SpawnEnemy(enemyGroup.EnemyType, Vector3.zero, Quaternion.identity);
        if (enemy)
            AddControllerToGroup(enemyGroup, enemy);
        return enemy;
    }

    public EnemyController SpawnEnemy(EnemyType enemyType, Vector3 position, Quaternion rotation)
    {
        EnemyController enemyController = controllerSpawner.SpawnController(enemyType.ToString(), position, rotation) as EnemyController;
        if (!enemyController) return null;

        availableControllers.Add(enemyController);
        spawnedEnemies.Add(enemyController);

        enemyController.enemyType = enemyType;
        enemyController.SetIdleTarget(GameManager.Instance != null ? GameManager.Instance.playerController : null);
        enemyController.Target = null;
        enemyController.State = enemyController.idlingState;
        enemyController.entity.AddCurrentHealth(enemyController.entity.stats.MaxHealth - enemyController.entity.CurrentHealth);
        enemyController.Consciousness = 100f;

        return enemyController;
    }
    
    public bool DespawnAllEnemiesAndStopCoroutines()
    {
        foreach (var enemyController in availableControllers.ToArray())
        {
            DespawnEnemy(enemyController);
        }
        foreach (var enemyController in groupCoroutines.Keys.SelectMany(g => g.EnemyList).ToArray())
        {
            DespawnEnemy(enemyController);
        }
        foreach (var coroutineList in groupCoroutines.Values)
        {
            foreach (var coroutine in coroutineList)
            {
                if (coroutine != null)
                    StopCoroutine(coroutine);
            }
        }
        groupCoroutines.Clear();
        availableControllers.Clear();
        return true;
    }
    private void DespawnEnemy(Controller controller)
    {
        EnemyController enemyController = controller as EnemyController;
        if (enemyController == null || !spawnedEnemies.Remove(enemyController)) return;

        foreach (EnemyGroup group in groupCoroutines.Keys.ToList())
        {
            if (group.EnemyList.Contains(enemyController))
            {
                group.EnemyList.Remove(enemyController);
                break;
            }
        }
        availableControllers.Remove(enemyController);

        enemyController.SetIdleTarget(null);
        controllerSpawner.DespawnController(enemyController.enemyType.ToString(), enemyController);
    }

    private IEnumerator PerformEnemyGroupAttackCoroutine(EnemyGroup enemyGroup)
    {
        while (true)
        {
            List<EnemyController> enemiesToPerformAttack = new List<EnemyController>();

            while (enemiesToPerformAttack.Count == 0)
            {
                var targetPosition = enemyGroup.TargetController.transform.position;

                if (enemyGroup.EnemyList.Count == 0)
                {
                    yield return new WaitForSeconds(0.1f);
                    continue;
                }

                var orderedEnemiesByDistance = enemyGroup.EnemyList
                            .OrderBy(c => (c.transform.position - targetPosition).sqrMagnitude)
                            .ToList();

                float attackRange = Mathf.Max(0f, enemyGroup.AttackRange);
                float attackRangeSquared = attackRange * attackRange;
                int numberOfSimultaneousAttackersCount = 0;

                for (int i = 0; i < orderedEnemiesByDistance.Count; i++)
                {
                    if (numberOfSimultaneousAttackersCount >= enemyGroup.MaxNumberOfEnemiesThatCanEngageSimultaneously)
                    {
                        break;
                    }

                    EnemyController enemyController = orderedEnemiesByDistance[i];
                    // Candidates are sorted by distance, so all remaining enemies are also out of range.
                    if ((enemyController.transform.position - targetPosition).sqrMagnitude > attackRangeSquared)
                        break;

                    if (enemyController.State == enemyController.pursuingState)
                    {
                        enemiesToPerformAttack.Add(enemyController);
                        numberOfSimultaneousAttackersCount++;
                    }
                }

                if (enemiesToPerformAttack.Count == 0)
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }

            foreach (EnemyController enemyController in enemiesToPerformAttack)
            {
                enemyController.Attack();
            }

            yield return new WaitForSeconds(enemyGroup.AttackInterval);
        }
    }

    private IEnumerator RefreshAttackingEnemyGroupCoroutine(EnemyGroup enemyGroup)
    {
        while (true)
        {
            // Keep attackers and link traversers in the group until they finish.
            List<EnemyController> attackers = enemyGroup.EnemyList
                .Where(e => e && (
                    e.State == e.climbingState ||
                    e.ActionInstances.Any(a => e.State == a.preparing || e.State == a.executing)))
                .Distinct()
                .ToList();
            List<EnemyController> closestEnemyControllers = attackers.Concat(
                GetClosestEnemyControllers(enemyGroup.EnemyType,
                    Mathf.Max(0, enemyGroup.MaxNumberOfEnemies - attackers.Count),
                    enemyGroup.TargetController.transform.position, enemyGroup.MaxEngagementDistance))
                .ToList();

            List<EnemyController> enemiesToRemove = enemyGroup.EnemyList.Except(closestEnemyControllers).ToList();
            List<EnemyController> enemiesToAdd = closestEnemyControllers.Except(enemyGroup.EnemyList).ToList();

            foreach (EnemyController enemyController in enemiesToRemove)
            {
                RemoveControllerFromGroup(enemyGroup, enemyController);
            }

            foreach (EnemyController enemyController in enemiesToAdd)
            {
                AddControllerToGroup(enemyGroup, enemyController);
            }

            yield return new WaitForSeconds(refreshEnemyFollowGroupInterval);
        }
    }

    private List<EnemyController> GetClosestEnemyControllers(EnemyType enemyType, int maxNumberOfEnemies, Vector3 position, float radius)
    {
        return Physics.OverlapSphere(position, radius)
            .Select(c => c.GetComponent<EnemyController>())
            .Where(e => e != null && e.enemyType == enemyType && (e.State == e.idlingState || e.State == e.pursuingState))
            .Distinct()
            .OrderBy(e => (e.transform.position - position).sqrMagnitude)
            .Take(maxNumberOfEnemies)
            .ToList();
    }

    private void RemoveControllerFromGroup(EnemyGroup enemyGroup, EnemyController enemyController)
    {
        if (enemyGroup.EnemyList.Contains(enemyController))
        {
            enemyGroup.EnemyList.Remove(enemyController);
            if (!enemyController) return;
         
            enemyController.SetIdleTarget(enemyGroup.TargetController);
            enemyController.Target = null;
            if (enemyController.navMeshAgent && enemyController.State == enemyController.pursuingState)
            {
                enemyController.State = enemyController.idlingState;
            }
            if (enemyController) availableControllers.Add(enemyController);
        }
    }

    private void AddControllerToGroup(EnemyGroup enemyGroup, EnemyController enemyController)
    {
        if (!enemyGroup.EnemyList.Contains(enemyController))
        {
            enemyGroup.EnemyList.Add(enemyController);
            enemyController.SetIdleTarget(enemyGroup.TargetController);
            enemyController.Target = enemyGroup.TargetController;
            if (enemyController.State == enemyController.idlingState)
            {
                enemyController.State = enemyController.pursuingState;
            }
            if (enemyController) availableControllers.Remove(enemyController);
        }
    }
}
