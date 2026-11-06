using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyAnimationController : MonoBehaviour
{
    public EnemyController enemyController;
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerAnimationData animationData = new();
    [Header("Climbing wall contact")]
    [SerializeField] private LayerMask climbWallMask = Physics.DefaultRaycastLayers;
    [SerializeField, Min(0.01f)] private float climbCastRadius = 0.25f;
    [SerializeField, Min(0.01f)] private float climbCastDistance = 2f;
    [Tooltip("Distance from the wall surface to the animated body's pivot.")]
    [SerializeField, Min(0f)] private float climbWallOffset = 0.4f;
    private Vector3 heightOffset = Vector3.up;
    private Quaternion currentTilt = Quaternion.identity;
    private Quaternion climbingRotation;
    private bool hasClimbingRotation;
    private Vector3 climbWallDirection;
    private readonly RaycastHit[] climbWallHits = new RaycastHit[16];
    private static int HorizontalSpeed, Pursuing, Climbing, Preparing, Executing, ActionIndex;
    private static readonly int ActionVariationIndex = Animator.StringToHash("ActionVariationIndex");

    public void SetActionVariation(int index)
    {
        animator.SetInteger(ActionVariationIndex, index);
    }

    void Awake()
    {
        animator = GetComponent<Animator>();

        HorizontalSpeed = Animator.StringToHash("HorizontalSpeed");
        Pursuing = Animator.StringToHash("Pursuing");
        Climbing = Animator.StringToHash("Climbing");
        Preparing = Animator.StringToHash("Preparing");
        Executing = Animator.StringToHash("Executing");
        ActionIndex = Animator.StringToHash("ActionIndex");

    }

    void Start()
    {
        SetOnFixedUpdateState();
    }

    private void SetOnFixedUpdateState()
    {
        enemyController.idlingState.OnFixedUpdate += () =>
        {
            UpdateBodyPosition(heightOffset);
            UpdateBodyRotation();
        };

        enemyController.pursuingState.OnFixedUpdate += () =>
        {
            UpdateBodyPosition(heightOffset);
            UpdateBodyRotation();
        };

        enemyController.climbingState.OnFixedUpdate += () =>
        {
            UpdateClimbingBodyRotation();
            UpdateClimbBodyPosition();
        };

    }

    void Update()
    {
        UpdateAnimationFloatValues();
    }   

    private void UpdateAnimationFloatValues()
    {
        animator.SetFloat(HorizontalSpeed, new Vector3(enemyController.navMeshAgent.velocity.x, 0f, enemyController.navMeshAgent.velocity.z).magnitude);
    }

    private void UpdateState(State state)
    {
        hasClimbingRotation = false;
        animator.SetBool(Pursuing, state == enemyController.pursuingState);
        animator.SetBool(Climbing, state == enemyController.climbingState);

        animator.SetBool(Preparing, enemyController.State.Type == StateType.PREPARING);
        animator.SetBool(Executing, enemyController.State.Type == StateType.EXECUTING);
    }

    private void UpdateBodyPosition(Vector3 offset)
    {
        Transform model = transform;
        Transform controllerTransform = enemyController.transform;
        Vector3 targetPosition = controllerTransform.position + offset;

        model.position = Vector3.Lerp(
            model.position,
            targetPosition,
            animationData.positionFollowSpeed * Time.fixedDeltaTime
        );
    }

    private void UpdateClimbBodyPosition()
    {
        var agent = enemyController.navMeshAgent;
        if (enemyController.State != enemyController.climbingState || !hasClimbingRotation ||
            agent == null || !agent.enabled || !agent.isOnNavMesh || !agent.isOnOffMeshLink) return;

        Vector3 targetPosition = enemyController.transform.position + heightOffset;
        float radius = Mathf.Max(0.01f, climbCastRadius);
        // Start behind the desired body position so the sphere can approach the wall.
        float backoff = radius + climbWallOffset + 0.05f;
        Vector3 origin = targetPosition - climbWallDirection * backoff;
        int count = Physics.SphereCastNonAlloc(origin, radius, climbWallDirection, climbWallHits,
            Mathf.Max(0.01f, climbCastDistance) + backoff, climbWallMask, QueryTriggerInteraction.Ignore);

        int closest = -1;
        float closestDistance = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = climbWallHits[i];
            // Ignore initial overlaps, floors, and the enemy's own colliders.
            if (hit.distance <= 0f || Mathf.Abs(hit.normal.y) > 0.5f ||
                Vector3.Dot(hit.normal, climbWallDirection) >= -0.1f ||
                hit.collider.transform.IsChildOf(enemyController.transform) ||
                hit.collider.transform.IsChildOf(transform)) continue;
            if (hit.distance >= closestDistance) continue;
            closest = i;
            closestDistance = hit.distance;
        }

        if (closest >= 0)
        {
            RaycastHit hit = climbWallHits[closest];
            // Project onto the contact plane while preserving progress along the wall.
            float distanceToWall = Vector3.Dot(targetPosition - hit.point, hit.normal);
            targetPosition += hit.normal * (climbWallOffset - distanceToWall);
        }

        // Fall back to following the controller if the cast misses at the ledge.
        transform.position = Vector3.Lerp(transform.position, targetPosition,
            1f - Mathf.Exp(-animationData.positionFollowSpeed * Time.fixedDeltaTime));
    }

    private void UpdateBodyRotation()
    {
        Vector3 velocity = enemyController.navMeshAgent.velocity;
        if (velocity.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(velocity.normalized, Vector3.up);
            currentTilt = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            transform.rotation = currentTilt;
        }
    }

    private void UpdateClimbingBodyRotation()
    {
        var agent = enemyController.navMeshAgent;
        if (enemyController.State != enemyController.climbingState || agent == null ||
            !agent.enabled || !agent.isOnNavMesh || !agent.isOnOffMeshLink) return;

        if (!hasClimbingRotation)
        {
            var link = agent.currentOffMeshLinkData;
            Vector3 direction = link.endPos - link.startPos;
            // Approximate the wall-facing direction once for this climb.
            if (direction.y < 0f) direction = -direction;
            Vector3 facing = Vector3.ProjectOnPlane(direction, Vector3.up);
            if (facing.sqrMagnitude <= 0.0001f)
                facing = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (facing.sqrMagnitude <= 0.0001f)
                facing = Vector3.ProjectOnPlane(enemyController.transform.forward, Vector3.up);
            if (facing.sqrMagnitude <= 0.0001f) facing = Vector3.forward;

            climbWallDirection = facing.normalized;
            climbingRotation = Quaternion.LookRotation(facing, Vector3.up) * Quaternion.Euler(-90f, 0f, 0f);
            hasClimbingRotation = true;
        }

        currentTilt = Quaternion.Slerp(transform.rotation, climbingRotation,
            1f - Mathf.Exp(-5f * Time.fixedDeltaTime));
        transform.rotation = currentTilt;
    }

    private void ActionValueChanged(ActionInstance action)
    {
        animator.SetInteger(ActionIndex, enemyController.ActionInstances.IndexOf(action));
    }

    private void OnEnable()
    {
        hasClimbingRotation = false;
        currentTilt = transform.rotation;
        enemyController.OnActionInstancePerformed += ActionValueChanged;
        enemyController.OnStateChanged += UpdateState;
    }

    private void OnDisable()
    {
        enemyController.OnActionInstancePerformed -= ActionValueChanged;
        enemyController.OnStateChanged -= UpdateState;
    }
}

