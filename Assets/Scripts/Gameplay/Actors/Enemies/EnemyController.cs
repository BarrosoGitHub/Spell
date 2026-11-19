using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : Controller
{
    public NavMeshAgent navMeshAgent;
    public State idlingState = new State("Idling");
    public State pursuingState = new State("Pursuing");
    public State climbingState = new State("Climbing");
    public State deadState = new State("Dead");

    private float pathTimer;

    [SerializeField, Min(0f)] private float idleStoppingDistance = 3f;
    private const float MinIdleAvoidanceMultiplier = 1.3f;
    private const float MaxIdleAvoidanceMultiplier = 3f;
    private Controller idleTarget;
    private float defaultAvoidanceRadius;
    private float pursuitStoppingDistance;

    public void SetIdleTarget(Controller controller)
    {
        idleTarget = controller;
    }

    public EnemyType enemyType;
    [Tooltip("Movement speed across NavMesh links, in units per second.")]
    [Min(0.1f)] public float jumpSpeed = 0.1f;
    private bool savedUpdatePosition;
    protected override void Awake()
    {
        base.Awake();
        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.autoTraverseOffMeshLink = false;
        pursuitStoppingDistance = navMeshAgent.stoppingDistance;
        defaultAvoidanceRadius = navMeshAgent.radius;

    }

    public override void Start()
    {
        base.Start();
        State?.OnEnterState?.Invoke();
    }

    public override void SetOnEnterState()
    {
        idlingState.OnEnterState += () =>
        {
            navMeshAgent.enabled = true;
            if (navMeshAgent.isOnNavMesh) navMeshAgent.isStopped = false;
            navMeshAgent.stoppingDistance = idleStoppingDistance;
            navMeshAgent.radius = defaultAvoidanceRadius * Random.Range(MinIdleAvoidanceMultiplier, MaxIdleAvoidanceMultiplier);
        };
        pursuingState.OnEnterState += () =>
        {
            navMeshAgent.enabled = true;
            if (navMeshAgent.isOnNavMesh) navMeshAgent.isStopped = false;
            navMeshAgent.stoppingDistance = pursuitStoppingDistance;
        };

        climbingState.OnEnterState += () =>
        {
            savedUpdatePosition = navMeshAgent.updatePosition;
            navMeshAgent.updatePosition = false;
        };

        deadState.OnEnterState += () =>
        {
            if (navMeshAgent != null)
            {
                navMeshAgent.enabled = false;
            }
            Target = null;
        };

    }

    public override void SetOnExitState()
    {
        idlingState.OnExitState += () => navMeshAgent.radius = defaultAvoidanceRadius;
        climbingState.OnExitState += () => navMeshAgent.updatePosition = savedUpdatePosition;
    }
    public override void SetOnUpdateState() { }

    public override void SetOnFixedUpdateState()
    {
        idlingState.OnFixedUpdate += () =>
        {
            if (idleTarget == null) return;
            UpdateNavigation(idleTarget.transform.position);
        };

        pursuingState.OnFixedUpdate += () =>
        {
            if (Target == null) return;
            UpdateNavigation(Target.transform.position);
        };
        climbingState.OnFixedUpdate += UpdateLinkTraversal;

    }

    private void UpdateNavigation(Vector3 position)
    {
        if (!navMeshAgent.enabled || !navMeshAgent.isOnNavMesh) return;

        pathTimer += Time.fixedDeltaTime;
        if (pathTimer < 0.25f) return;

        pathTimer = 0f;
        navMeshAgent.SetDestination(position);
    }

    private void UpdateLinkTraversal()
    {
        if (!navMeshAgent.enabled || !navMeshAgent.isOnNavMesh || !navMeshAgent.isOnOffMeshLink)
        {
            State = Target != null ? pursuingState : idlingState;
            return;
        }

        Vector3 destination = navMeshAgent.currentOffMeshLinkData.endPos + Vector3.up * navMeshAgent.baseOffset;
        transform.position = Vector3.MoveTowards(transform.position, destination,
            Mathf.Max(0.1f, jumpSpeed) * Time.fixedDeltaTime);
        if (Vector3.Distance(transform.position, destination) > 0.01f) return;

        navMeshAgent.CompleteOffMeshLink();
        State = Target != null ? pursuingState : idlingState;
    }

    public override void SetActions()
    {
        ActionInstances = new List<ActionInstance>
        {
            new AttackDirectional(this, 0.9f, 0.5f),
            new GettingUp(this, 2f, 2f)
        };
    }

    public void Attack()
    {
        if (State != idlingState && State != pursuingState) return;
        if (State == climbingState || navMeshAgent.isOnOffMeshLink) return;
        if (ActionInstances == null || ActionInstances.Count == 0) return;
        // Directional attack is the first action; GettingUp is reserved for recovery.
        PerformAction(ActionInstances[0]);
    }

    protected virtual void FixedUpdate()
    {
        navMeshAgent.speed = Mathf.Max(0f, entity.stats.MovementSpeed);
        if ((State == pursuingState || State == idlingState) && navMeshAgent.enabled &&
            navMeshAgent.isOnNavMesh && navMeshAgent.isOnOffMeshLink)
            State = climbingState;

        State?.OnFixedUpdate?.Invoke();
    }
    public override void OnDied()
    {
        base.OnDied();

        State = deadState;
    }
}
