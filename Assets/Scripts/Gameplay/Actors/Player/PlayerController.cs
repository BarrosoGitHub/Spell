using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : Controller
{
    private PlayerControls inputControls;

    [Header("Slow Motion")]
    [Tooltip("Game speed while holding the right mouse button. 0.05 is 20 times slower.")]
    [SerializeField, Range(0.01f, 1f)] private float slowMotionTimeScale = 0.05f;
    private bool slowMotionActive;
    private float previousTimeScale;
    private float previousFixedDeltaTime;
    private float baselineFixedDeltaTime;

    [SerializeField] private Transform cam;
    [Tooltip("World surfaces and targets that the mouse can aim at. Excludes triggers and the player.")]
    [SerializeField] private LayerMask aimMask = Physics.DefaultRaycastLayers;
    public PlayerActionsSO playerActionsSO;

    private Vector2 inputDirection;
    private Vector3 horizontalDirection;
    public Vector3 moveDirection;

    public bool isGrounded;
    public bool IsGrounded
    {
        get
        {
            return isGrounded;
        }
        set
        {
            if (isGrounded == value) return;

            isGrounded = value;
            OnGroundedChanged?.Invoke(isGrounded);
        }
    }

    public State movingState, hangingState;

    public LayerMask groundMask;
    [SerializeField] private MovementData movementData = MovementData.Default;
    // Retain the serialized gravity settings used by existing players.
    [SerializeField] private JumpData jumpData = JumpData.Default;
    [SerializeField] private HoverData hoverData = HoverData.Default;
    [SerializeField] private ClimbingData climbingData = ClimbingData.Default;
    public RaycastHit checkHit, groundHit;

    public GameObject closestHold;

    private readonly HashSet<HoldSelectionDebug> highlightedHolds = new HashSet<HoldSelectionDebug>();
    private readonly HashSet<HoldSelectionDebug> reachableHolds = new HashSet<HoldSelectionDebug>();

    protected override void Awake()
    {
        baselineFixedDeltaTime = Time.fixedDeltaTime;
        base.Awake();
        cam = Camera.main.transform;

        inputControls = new PlayerControls();

        SetStates();
    }

    public override void Start()
    {
        base.Start();
    }

    private void SetStates()
    {
        movingState = new State();
        hangingState = new State();

        State = movingState;
    }

    public override void SetOnEnterState()
    {
        movingState.OnEnterState += () =>
        {
            // StateVariation = false;
            col.enabled = true;
        };

        hangingState.OnEnterState += () =>
        {
            col.enabled = false;
        };

        ActionInstances[0].preparing.OnEnterState += () =>
        {
            Vector3 surfaceNormal = CalculationHelper.GetSurfaceForwardFromHold(closestHold.transform);
            CheckFreeHang(closestHold.transform.position + Vector3.down, -surfaceNormal);
        };

        ActionInstances[3].preparing.OnEnterState += () =>
        {
            Vector3 surfaceNormal = CalculationHelper.GetSurfaceForwardFromHold(closestHold.transform);
            CheckFreeHang(closestHold.transform.position + Vector3.down, -surfaceNormal);
        };
    }

    public override void SetOnExitState()
    {

    }

    public override void SetOnUpdateState()
    {
        movingState.OnUpdate += () =>
        {
            MovementDirection();
            ManageParkour();
            ManageWeapon();
            ManageDodge();
        };

        hangingState.OnUpdate += () =>
        {
            ManageHang();
            ManageWeapon();
        };
    }

    public override void SetOnFixedUpdateState()
    {
        ActionInstances[4].preparing.OnFixedUpdate += ManageHover;
        ActionInstances[4].executing.OnFixedUpdate += ManageHover;

        movingState.OnFixedUpdate += () =>
        {
            PhysicsUpdate(movementData.drag, movementData.maxSpeed);
        };
    }

    public override void SetActions()
    {
        ActionInstances = new List<ActionInstance>
        {
            new Grab(this, playerActionsSO.Grab),
            new ClimbUp(this, playerActionsSO.ClimbUp),
            new ClimbEject(this, playerActionsSO.ClimbEject),
            new ClimbJump(this, playerActionsSO.ClimbJump),
            new Dodge(this, playerActionsSO.Dodge)
            // Add more actions here as needed
        };
    }

    public bool TryDodge()
    {
        if (!isActiveAndEnabled || rb == null || State != movingState ||
            CurrentActionInstance != null || entity.CurrentHealth <= 0f || Time.timeScale <= 0f)
            return false;

        PerformAction(ActionInstances[4]);
        return true;
    }

    private void Update()
    {
        ManageSlowMotion();
        State.OnUpdate?.Invoke();

        ManageInput();
        HighlightGrabbableHolds();

        Debug.DrawRay(transform.position, transform.forward, Color.red);
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        State?.OnFixedUpdate?.Invoke();
    }

    private void ManageSlowMotion()
    {
        bool rightClickHeld = Application.isFocused && Mouse.current != null && Mouse.current.rightButton.isPressed;
        if (!rightClickHeld)
        {
            RestoreTimeScale();
            return;
        }

        if (slowMotionActive || Time.timeScale <= 0f) return;

        previousTimeScale = Time.timeScale;
        previousFixedDeltaTime = Time.fixedDeltaTime;
        float scale = Mathf.Clamp(slowMotionTimeScale, 0.01f, 1f);
        Time.timeScale = previousTimeScale * scale;
        Time.fixedDeltaTime = previousFixedDeltaTime * scale;
        slowMotionActive = true;
    }

    private void RestoreTimeScale()
    {
        if (!slowMotionActive) return;

        Time.timeScale = previousTimeScale;
        Time.fixedDeltaTime = previousFixedDeltaTime;
        slowMotionActive = false;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) RestoreTimeScale();
    }

    private void PhysicsUpdate(Vector3 drag, float speed)
    {
        ManageHover();

        rb.AddForce(10f * speed * moveDirection, ForceMode.Acceleration);
        Vector3 velocity = rb.linearVelocity;
        float dragTimeScale = Time.fixedDeltaTime / baselineFixedDeltaTime;
        velocity.x *= Mathf.Pow(Mathf.Max(0f, drag.x), dragTimeScale);
        velocity.y *= Mathf.Pow(Mathf.Max(0f, drag.y), dragTimeScale);
        velocity.z *= Mathf.Pow(Mathf.Max(0f, drag.z), dragTimeScale);
        rb.linearVelocity = velocity;
    }

    private void ManageHover()
    {
        bool hasGround = Physics.SphereCast(
            transform.position,
            hoverData.groundCheckRadius,
            Vector3.down,
            out groundHit,
            hoverData.groundCheckDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        IsGrounded = hasGround && groundHit.distance <= hoverData.rideHeight + 0.3f;

        bool shouldHover = hasGround && rb.linearVelocity.y <= 0.5f;

        if (shouldHover)
        {
            float heightError = groundHit.distance - hoverData.rideHeight;

            float verticalVelocity =
                Vector3.Dot(rb.linearVelocity, Vector3.up);

            float springForce =
                (heightError * hoverData.springStrength) +
                (verticalVelocity * hoverData.springDamper);

            rb.AddForce(Vector3.down * springForce, ForceMode.Acceleration);
        }
        else
        {
            rb.AddForce(
                jumpData.gravityDirection * jumpData.gravityScale,
                ForceMode.Acceleration
            );
        }
    }

    private void ManageInput()
    {
        inputDirection = inputControls.Player.Move.ReadValue<Vector2>();
        inputDirection = Vector2.ClampMagnitude(inputDirection, 1f);

        horizontalDirection = new Vector3(inputDirection.x, 0f, inputDirection.y).normalized;
    }

    private void MovementDirection()
    {
        float angle = Mathf.Atan2(horizontalDirection.x, horizontalDirection.z) * Mathf.Rad2Deg + cam.eulerAngles.y;

        if (horizontalDirection.magnitude >= 0.1f)
        {
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
            moveDirection = Quaternion.Euler(0, angle, 0) * Vector3.forward;
        }
        else
        {
            moveDirection = Vector3.zero;
        }
    }

    private void ManageDodge()
    {
        if (!inputControls.Player.JumpPressed.triggered) return;

        TryDodge();
    }

    private void ManageParkour()
    {
        if (!inputControls.Player.ParkourPressed.IsPressed()) return;

        if (CanGrabLedge())
        {
            PerformAction(ActionInstances[0]);
            return;
        }
    }

    private void ManageHang()
    {
        Vector3 surfaceNormal = CalculationHelper.GetSurfaceForwardFromHold(closestHold.transform);
        Vector3 surfaceDown = CalculationHelper.GetSurfaceUpFromHold(closestHold.transform);

        // CheckFreeHang(closestHold.transform.position + Vector3.down, -surfaceNormal);

        Debug.DrawRay(transform.position, -surfaceNormal, Color.magenta);

        // if (StateVariation)
        // {
        //     print("here");
        //     transform.position = closestHold.transform.position + Vector3.ProjectOnPlane(surfaceDown.normalized, transform.right).normalized * 1.12f;
        // }
        // else
        // {
        //     transform.position = closestHold.transform.position + Vector3.ProjectOnPlane(surfaceDown.normalized, transform.right).normalized * 0.77f + surfaceNormal * 0.3f;
        // }

        // transform.rotation = Quaternion.LookRotation(-surfaceNormal);

        if (GetRelativeDirection() == Vector3.zero)
        {
            return;
        }

        if (IsClimbUpPossible())
        {
            PerformAction(ActionInstances[1]);
            return;
        }

        if (inputControls.Player.JumpPressed.IsPressed())
        {
            
            PerformAction(ActionInstances[2]);
            return;
        }

        if (IsClimbJumpPossible())
        {
            PerformAction(ActionInstances[3]);
        }
    }

    private void ManageWeapon()
    {
        if (weaponHandler == null || weaponHandler.EquippedWeapon == null)
            return;

        if (inputControls.Player.AttackPressed.IsPressed())
        {
            Weapon weapon = weaponHandler.EquippedWeapon;
            if (CalculationHelper.TryGetMouseAimPoint(this, weapon, cam, aimMask, out Vector3 aimPoint))
            {
                var (target, context) = weaponHandler.TryExecuteAttack(this, aimPoint);
                if (target != null && context != null)
                    AttackController(target, context);
            }
        }
    }

    private bool IsClimbUpPossible()
    {
        float climbUpAngle = Vector3.Angle(GetRelativeDirection(), closestHold.transform.forward);
        if (climbUpAngle < 45 || climbUpAngle > 70) return false;

        Ray topRay = new(transform.position + Vector3.up * 1.25f, transform.forward);

        if (!Physics.SphereCast(topRay, 0.05f, 0.5f, groundMask.value))
        {
            Ray checkRay = new(closestHold.transform.position + Vector3.up * 0.5f + transform.forward * 0.1f, Vector3.down);
            if (Physics.Raycast(checkRay, out checkHit, 1, groundMask)) return true;
        }
        return false;
    }

    private bool IsClimbJumpPossible()
    {
        GameObject newClosestHold = CalculationHelper.GetClosestHoldForClimbing(transform.position + Vector3.up * 0.75f + GetRelativeClimbDirection() * climbingData.jumpDistance, 0.75f, closestHold);

        if (newClosestHold == null)
        {
            return false;
        }
        closestHold = newClosestHold;
        return true;
    }

    /// <summary>Colors every hold in grab range green while it passes the current grab rules.</summary>
    public void HighlightGrabbableHolds()
    {
        reachableHolds.Clear();
        if (State == movingState && !isGrounded)
        {
            Vector3 center = transform.position + Vector3.up * 0.75f;
            Vector3 target = transform.position + GetRelativeDirection();
            foreach (Collider hold in Physics.OverlapSphere(center, 0.6f, 64))
            {
                if (!CalculationHelper.CanGrabHold(hold.gameObject, target, transform, closestHold)) continue;
                HoldSelectionDebug highlight = hold.GetComponent<HoldSelectionDebug>();
                if (!highlight) highlight = hold.gameObject.AddComponent<HoldSelectionDebug>();
                reachableHolds.Add(highlight);
                highlight.SetGrabbable(true);
            }
        }

        foreach (HoldSelectionDebug highlight in highlightedHolds)
            if (highlight && !reachableHolds.Contains(highlight)) highlight.SetGrabbable(false);

        highlightedHolds.Clear();
        foreach (HoldSelectionDebug highlight in reachableHolds) highlightedHolds.Add(highlight);
    }

    private bool CanGrabLedge()
    {
        if (isGrounded) return false;

        closestHold = CalculationHelper.GetClosestEdgeObject(transform.position + Vector3.up * 0.75f, transform.position + GetRelativeDirection(), transform, 0.6f, closestHold);
        if (closestHold != null)
        {
            Vector3 surfaceNormal = CalculationHelper.GetSurfaceNormalFromHold(closestHold.transform);
            Vector3 movePos = closestHold.transform.position + Vector3.down * 0.75f + surfaceNormal * 0.25f;
            // player.stateVariant = CheckFreeHang(movePos, -surfaceNormal);
            return true;
        }
        return false;
    }

    private void CheckFreeHang(Vector3 position, Vector3 direction)
    {

        // // Apply the rotation to the object, preserving the current X and Z rotations
        //     if (StateVariation)
        //     {
        //         if (Physics.Raycast(position - direction * 0.02f, direction, 0.35f, groundMask))
        //         {
        //             StateVariation = false;
        //             print("Changed");
        //         }
        //     }
        //     else
        //     {
        //         if (!Physics.Raycast(position - direction * 0.2f, direction, 0.5f, groundMask))
        //         {
        //             StateVariation = true;
        //         }

        //     }
    }

    public Vector3 GetRelativeDirection()
    {
        return Quaternion.Euler(0, cam.eulerAngles.y, 0) * horizontalDirection;
    }

    public Vector3 GetRelativeClimbDirection()
    {
        float angleBetweenDirectionAndFront = Vector3.Angle(transform.forward, GetRelativeDirection());

        if (inputControls.Player.ParkourPressed.IsPressed() || angleBetweenDirectionAndFront < 90)
        {
            return (Quaternion.AngleAxis(-90, transform.right) * GetRelativeDirection()).normalized;
        }
        else
        {
            return GetRelativeDirection();
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        inputControls?.Enable();
    }

    protected override void OnDisable()
    {
        RestoreTimeScale();
        base.OnDisable();
        inputControls?.Disable();
        foreach (HoldSelectionDebug highlight in highlightedHolds)
            if (highlight) highlight.SetGrabbable(false);
        highlightedHolds.Clear();
        reachableHolds.Clear();
    }

    private void OnDestroy()
    {
        inputControls?.Dispose();
    }
}
