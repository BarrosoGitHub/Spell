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

    [SerializeField, Min(0f)] private float idleStoppingDistance = 10f;
    private Controller idleTarget;
    private float pursuitStoppingDistance;

    public void SetIdleTarget(Controller controller)
    {
        idleTarget = controller;
    }

    public EnemyType enemyType;
    [Tooltip("Movement speed across NavMesh links, in units per second.")]
    [Min(0.1f)] public float jumpSpeed = 0.1f;
    private bool savedUpdatePosition;
    protected override void UpdateConsciousnessState()
    {
        if (State == deadState || (entity != null && entity.CurrentHealth <= 0f)) return;
        base.UpdateConsciousnessState();
    }

    protected override void Awake()
    {
        base.Awake();
        if (GetComponent<EnemyPursuitIndicator>() == null)
            gameObject.AddComponent<EnemyPursuitIndicator>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.autoTraverseOffMeshLink = false;
        pursuitStoppingDistance = navMeshAgent.stoppingDistance;

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
        };
        pursuingState.OnEnterState += () =>
        {
            navMeshAgent.enabled = true;
            if (navMeshAgent.isOnNavMesh) navMeshAgent.isStopped = false;
            navMeshAgent.speed = 2.5f;
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

        unconsciousState.OnEnterState += () =>
        {
            navMeshAgent.enabled = false;
        };
    }

    public override void SetOnExitState()
    {
        climbingState.OnExitState += () => navMeshAgent.updatePosition = savedUpdatePosition;
    }
    public override void SetOnUpdateState() { }

    public override void SetOnFixedUpdateState()
    {
        idlingState.OnFixedUpdate += () =>
        {
            if (idleTarget == null || !navMeshAgent.enabled || !navMeshAgent.isOnNavMesh) return;
            navMeshAgent.SetDestination(idleTarget.transform.position);
        };

        pursuingState.OnFixedUpdate += UpdatePursuit;
        climbingState.OnFixedUpdate += UpdateLinkTraversal;

        unconsciousState.OnFixedUpdate += () =>
        {
            if (Consciousness == 100f)
            {
                PerformAction(ActionInstances.Find(a => a is GettingUp));
            }
        };
    }

    private void UpdatePursuit()
    {
        if (Target == null || !navMeshAgent.enabled || !navMeshAgent.isOnNavMesh) return;
        navMeshAgent.SetDestination(Target.transform.position);
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

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
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
