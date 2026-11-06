using UnityEngine;
using UnityEngine.Serialization;

public class PlayerAnimationController : MonoBehaviour, ISerializationCallbackReceiver
{
    public PlayerController playerController;
    [SerializeField] private PlayerAnimationData animationData = new();
    [SerializeField, HideInInspector] private bool animationDataMigrated;

    // Legacy fields retain existing prefab/scene overrides until migrated to Animation Data.
    [SerializeField, HideInInspector] private float positionFollowSpeed = 12f;
    [SerializeField, HideInInspector] private float rotationFollowSpeed = 7f;
    [SerializeField, HideInInspector] private float accelerationTiltStrength = 0.12f;
    [SerializeField, HideInInspector] private float maxTiltAngle = 12f;
    [SerializeField, HideInInspector] private float tiltResponseSpeed = 4f;
    [SerializeField, HideInInspector] private float heightOffset = 1f;

    [SerializeField, HideInInspector] private float hangingPendulumLength = 0.9f;
    [FormerlySerializedAs("hangingDamping")]
    [SerializeField, HideInInspector] private float hangingForwardDamping = 0.35f;
    [SerializeField, HideInInspector] private float hangingSideDamping = 0.35f;
    [SerializeField, HideInInspector] private float hangingForwardAngleLimit = 22f;
    [SerializeField, HideInInspector] private float hangingBackwardAngleLimit = 22f;
    [FormerlySerializedAs("hangingMaxAngle")]
    [SerializeField, HideInInspector] private float hangingSideAngleLimit = 22f;
    [SerializeField, HideInInspector] private float hangingGrabImpulse = 0.35f;
    [SerializeField, HideInInspector] private float grabTiltAngle = 8f;
    [SerializeField, HideInInspector] private float hangingMovementStrength = 35f;
    private Vector3 pendingGrabVelocity;
    private bool wasClimbJump;
    private Vector2 hangingAngles, hangingAngularVelocity;
    private Vector3 hangingBasePosition, previousBodyPosition, previousBodyVelocity;
    private Quaternion hangingBaseRotation;
    private bool hangingPendulumActive;
    private HangingSpringState hangingSpring;
    private Vector3 springWorldOffset;
    private Animator animator;
    private Vector3 lastVelocity;
    private Quaternion currentTilt = Quaternion.identity;
    private static int HorizontalSpeed, VerticalSpeed, Grounded, Moving, Hanging, Preparing, Executing, ActionIndex;

    public void OnBeforeSerialize() { }
    public void OnAfterDeserialize() => MigrateAnimationData();
    private void OnValidate() => MigrateAnimationData();

    private void MigrateAnimationData()
    {
        animationData ??= new PlayerAnimationData();
        if (animationDataMigrated) return;
        animationData.positionFollowSpeed = positionFollowSpeed;
        animationData.rotationFollowSpeed = rotationFollowSpeed;
        animationData.accelerationTiltStrength = accelerationTiltStrength;
        animationData.maxTiltAngle = maxTiltAngle;
        animationData.tiltResponseSpeed = tiltResponseSpeed;
        animationData.heightOffset = heightOffset;
        animationData.hangingPendulumLength = hangingPendulumLength;
        animationData.hangingForwardDamping = hangingForwardDamping;
        animationData.hangingSideDamping = hangingSideDamping;
        animationData.hangingForwardAngleLimit = hangingForwardAngleLimit;
        animationData.hangingBackwardAngleLimit = hangingBackwardAngleLimit;
        animationData.hangingSideAngleLimit = hangingSideAngleLimit;
        animationData.hangingGrabImpulse = hangingGrabImpulse;
        animationData.grabTiltAngle = grabTiltAngle;
        animationData.hangingMovementStrength = hangingMovementStrength;
        animationDataMigrated = true;
    }

    void Awake()
    {
        animator = GetComponent<Animator>();

        HorizontalSpeed = Animator.StringToHash("HorizontalSpeed");
        VerticalSpeed = Animator.StringToHash("VerticalSpeed");
        Grounded = Animator.StringToHash("Grounded");
        Moving = Animator.StringToHash("Moving");
        Hanging = Animator.StringToHash("Hanging");
        Preparing = Animator.StringToHash("Preparing");
        Executing = Animator.StringToHash("Executing");
        ActionIndex = Animator.StringToHash("ActionIndex");
        // Variation = Animator.StringToHash("Variation");
        // Mirrored = Animator.StringToHash("Mirrored");
    }

    void Start()
    {
        SetOnEnterState();
        SetOnFixedUpdateState();
        SetOnExitState();
    }

    private void SetOnEnterState()
    {
        playerController.movingState.OnEnterState += () =>
        {
            transform.position = playerController.transform.position;
            lastVelocity = playerController.rb.linearVelocity;
        };

        playerController.ActionInstances[1].preparing.OnEnterState += () =>
        {
            animator.applyRootMotion = true;
            gameObject.InterpolatePosition(playerController.transform.position - playerController.transform.forward * 0.08f + Vector3.up * 0.25f, 0.1f);
        };

        playerController.ActionInstances[1].executing.OnEnterState += () =>
        {
            // animator.applyRootMotion = false;
            gameObject.InterpolatePosition(playerController.transform.position, 0.2f);
        };

    }

    private void SetOnFixedUpdateState()
    {
        playerController.movingState.OnFixedUpdate += () =>
        {
            UpdateBodyPosition(Vector3.down * animationData.heightOffset);
            UpdateBodyTilt();

            UpdateBodyRotation();
        };

        playerController.hangingState.OnFixedUpdate += UpdateHangingBody;

        playerController.ActionInstances[0].preparing.OnFixedUpdate += UpdateHangingBody;
        playerController.ActionInstances[0].executing.OnFixedUpdate += UpdateHangingBody;

        playerController.ActionInstances[1].preparing.OnFixedUpdate += UpdateClimbUpBody;
        playerController.ActionInstances[1].executing.OnFixedUpdate += UpdateClimbUpBody;

        playerController.ActionInstances[3].preparing.OnFixedUpdate += UpdateHangingBody;
        playerController.ActionInstances[3].executing.OnFixedUpdate += UpdateHangingBody;

        playerController.ActionInstances[2].preparing.OnFixedUpdate += () =>
        {
            UpdateBodyPosition(Vector3.zero);
            UpdateBodyRotation();
        };

    }

    private void SetOnExitState()
    {
        playerController.ActionInstances[1].executing.OnExitState += () =>
        {
            animator.applyRootMotion = false;
        };
    }

    private void Update()
    {
        UpdateAnimationFloatValues();
    }

    private void UpdateClimbUpBody()
    {
        // ClimbUp leaves the hanging simulation, so clear its cached swing and spring offsets.
        UpdateHangingBody();
        UpdateBodyRotation();
    }

    private void UpdateBodyPosition(Vector3 offset)
    {
        Transform model = transform;
        Transform controllerTransform = playerController.transform;
        Vector3 targetPosition = controllerTransform.position + offset;

        model.position = Vector3.Lerp(
            model.position,
            targetPosition,
            animationData.positionFollowSpeed * Time.fixedDeltaTime
        );
    }

    private void UpdateHangingBody()
    {
        bool flying = playerController.CurrentActionInstance is ClimbJump;
        bool grabbing = playerController.CurrentActionInstance is Grab;
        if (!flying && !grabbing && playerController.State != playerController.hangingState)
        {
            hangingPendulumActive = false;
            hangingSpring = default;
            springWorldOffset = Vector3.zero;
            return;
        }
        float dt = Time.fixedDeltaTime;
        if (dt <= 0f) return;
        Transform body = playerController.transform;
        float length = Mathf.Max(0.1f, animationData.hangingPendulumLength);
        Vector3 pivot = !flying && playerController.closestHold
            ? playerController.closestHold.transform.position : body.position + Vector3.up * length;
        Vector3 grabVelocity = pendingGrabVelocity;

        if (!hangingPendulumActive)
        {
            hangingBasePosition = transform.position;
            hangingBaseRotation = transform.rotation;
            hangingAngles = Vector2.zero;
            hangingSpring = default;
            springWorldOffset = Vector3.zero;
            Vector3 impact = body.InverseTransformDirection(pendingGrabVelocity);
            hangingAngularVelocity = Vector2.ClampMagnitude(new Vector2(
                -impact.z + Mathf.Max(0f, -impact.y) * 0.25f, impact.x)
                * (animationData.hangingGrabImpulse * Mathf.Rad2Deg / length), 120f);
            pendingGrabVelocity = Vector3.zero;
            previousBodyPosition = body.position;
            previousBodyVelocity = Vector3.zero;
            hangingPendulumActive = true;
            wasClimbJump = flying;
            currentTilt = Quaternion.identity;
        }
        if (wasClimbJump != flying)
        {
            Quaternion previousSwing = hangingBaseRotation * Quaternion.Euler(hangingAngles.x, 0f, hangingAngles.y)
                * Quaternion.Inverse(hangingBaseRotation);
            // Remove the previous spring displacement before recovering the unswung pose.
            hangingBasePosition = pivot + Quaternion.Inverse(previousSwing)
                * (transform.position - pivot - springWorldOffset);
            wasClimbJump = flying;
        }

        Vector3 radial = (hangingBasePosition - pivot).normalized;
        if (radial.sqrMagnitude < 0.001f) radial = Vector3.down;
        if (animationData.springEnabled)
            hangingSpring.velocity += Vector3.Dot(grabVelocity, radial) * Mathf.Max(0f, animationData.springGrabImpulse);
        else
            hangingSpring = default;
        pendingGrabVelocity = Vector3.zero;

        // Transform movement includes climb-jump tweens, whose rigidbody velocity is zero.
        Vector3 velocity = (body.position - previousBodyPosition) / dt;
        Vector3 acceleration = body.InverseTransformDirection(Vector3.ClampMagnitude(
            (velocity - previousBodyVelocity) / dt, 30f));
        previousBodyPosition = body.position;
        previousBodyVelocity = velocity;
        Vector3 input = flying || grabbing ? Vector3.zero : body.InverseTransformDirection(playerController.GetRelativeDirection());
        Vector2 targetTilt = Vector2.zero;
        if (grabbing && playerController.closestHold)
        {
            Vector3 towardLedge = body.InverseTransformDirection(playerController.closestHold.transform.position - body.position);
            Vector2 direction = new Vector2(-towardLedge.z, towardLedge.x).normalized;
            targetTilt = direction * animationData.grabTiltAngle;
        }
        float gravity = 9.81f / length;
        Vector2 damping = new Vector2(animationData.hangingForwardDamping, animationData.hangingSideDamping) * (2f * Mathf.Sqrt(gravity));
        Vector2 drive = new Vector2(-input.z, input.x) * animationData.hangingMovementStrength
            + new Vector2(acceleration.z, -acceleration.x) * (Mathf.Rad2Deg / length);
        float springDrive = -Vector3.Dot(body.TransformDirection(acceleration), radial)
            * Mathf.Max(0f, animationData.springAccelerationStrength)
            + input.magnitude * Mathf.Max(0f, animationData.springMovementStrength);

        for (float remaining = Mathf.Min(dt, 0.1f); remaining > 0f;)
        {
            float step = Mathf.Min(remaining, 1f / 120f);
            hangingSpring.Step(animationData, springDrive, step);
            Vector2 restoring = new Vector2(Mathf.Sin((hangingAngles.x - targetTilt.x) * Mathf.Deg2Rad),
                Mathf.Sin((hangingAngles.y - targetTilt.y) * Mathf.Deg2Rad)) * (gravity * Mathf.Rad2Deg);
            hangingAngularVelocity += (drive - restoring - Vector2.Scale(damping, hangingAngularVelocity)) * step;
            hangingAngles += hangingAngularVelocity * step;
            for (int axis = 0; axis < 2; axis++)
            {
                float limited = Mathf.Clamp(hangingAngles[axis],
                    axis == 0 ? -animationData.hangingForwardAngleLimit : -animationData.hangingSideAngleLimit,
                    axis == 0 ? animationData.hangingBackwardAngleLimit : animationData.hangingSideAngleLimit);
                if (limited != hangingAngles[axis]) hangingAngularVelocity[axis] = 0f;
                hangingAngles[axis] = limited;
            }
            remaining -= step;
        }
        hangingBasePosition = Vector3.Lerp(hangingBasePosition, body.position, 1f - Mathf.Exp(-animationData.positionFollowSpeed * dt));
        hangingBaseRotation = Quaternion.Slerp(hangingBaseRotation, body.rotation, 1f - Mathf.Exp(-animationData.rotationFollowSpeed * dt));
        Quaternion swing = hangingBaseRotation * Quaternion.Euler(hangingAngles.x, 0f, hangingAngles.y)
            * Quaternion.Inverse(hangingBaseRotation);
        Vector3 radius = hangingBasePosition - pivot;
        radial = radius.sqrMagnitude > 0.0001f ? radius.normalized : Vector3.down;
        // Compression cannot pull the body through the pivot.
        float springOffset = Mathf.Max(hangingSpring.offset, -Mathf.Max(0f, radius.magnitude - 0.01f));
        springWorldOffset = swing * radial * springOffset;
        transform.SetPositionAndRotation(pivot + swing * radius + springWorldOffset, swing * hangingBaseRotation);
    }
    private void UpdateBodyRotation()
    {
        Transform model = transform;
        Transform controller = playerController.transform;

        Quaternion targetRotation = Quaternion.LookRotation(controller.forward, Vector3.up) * currentTilt;

        model.rotation = Quaternion.Slerp(
            model.rotation,
            targetRotation,
            animationData.rotationFollowSpeed * Time.fixedDeltaTime
        );

        lastVelocity = playerController.rb.linearVelocity;
    }

    private void UpdateBodyTilt()
    {
        Vector3 worldAcceleration = (playerController.rb.linearVelocity - lastVelocity) / Time.fixedDeltaTime;
        Vector3 localAcceleration = playerController.transform.InverseTransformDirection(worldAcceleration);

        float pitchAngle = Mathf.Clamp(
            animationData.accelerationTiltStrength,
            -animationData.maxTiltAngle,
            animationData.maxTiltAngle
        ); float rollAngle = Mathf.Clamp(-localAcceleration.x * animationData.accelerationTiltStrength, -animationData.maxTiltAngle, animationData.maxTiltAngle);

        Quaternion targetTilt = Quaternion.Euler(pitchAngle, 0f, rollAngle);

        currentTilt = Quaternion.Slerp(
            currentTilt,
            targetTilt,
            animationData.tiltResponseSpeed * Time.fixedDeltaTime
        );
    }

    private void UpdateState(State state)
    {
        animator.SetBool(Moving, state == playerController.movingState);
        animator.SetBool(Hanging, state == playerController.hangingState);

        animator.SetBool(Preparing, playerController.State.Type == StateType.PREPARING);
        animator.SetBool(Executing, playerController.State.Type == StateType.EXECUTING);
    }

    private void UpdateAnimationFloatValues()
    {
        animator.SetFloat(HorizontalSpeed, new Vector3(playerController.rb.linearVelocity.x, 0, playerController.rb.linearVelocity.z).magnitude);
        animator.SetFloat(VerticalSpeed, playerController.rb.linearVelocity.y);
    }

    private void GroundedValueChanged(bool isGrounded)
    {
        animator.SetBool(Grounded, isGrounded);
    }

    private void ActionValueChanged(ActionInstance action)
    {
        if (action is Grab) pendingGrabVelocity = playerController.rb.linearVelocity;
        animator.SetInteger(ActionIndex, playerController.ActionInstances.IndexOf(action));
    }
    private void OnEnable()
    {
        playerController.OnGroundedChanged += GroundedValueChanged;
        playerController.OnActionInstancePerformed += ActionValueChanged;
        playerController.OnStateChanged += UpdateState;
    }

    private void OnDisable()
    {
        hangingPendulumActive = false;
        hangingSpring = default;
        springWorldOffset = Vector3.zero;
        pendingGrabVelocity = Vector3.zero;
        playerController.OnGroundedChanged -= GroundedValueChanged;
        playerController.OnActionInstancePerformed -= ActionValueChanged;
        playerController.OnStateChanged -= UpdateState;
    }
}
