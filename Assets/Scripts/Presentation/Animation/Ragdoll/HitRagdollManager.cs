using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>Standalone humanoid ragdoll: setup in .Setup.cs, active control here.</summary>
[DisallowMultipleComponent, DefaultExecutionOrder(100)]
public sealed partial class HitRagdollManager : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private Animator animator;
    [Tooltip("Parent for generated hitboxes. Use the gameplay capsule so damage can find its owner.")]
    [SerializeField] private Transform physicsParent;
    [Tooltip("Optional gameplay capsule. Becomes a trigger while the ragdoll owns collision.")]
    [SerializeField] private Collider rootCollider;
    private const float elbowFlexionLimit = 145f;
    private const float kneeFlexionLimit = 135f;

    [Header("Animation strength — 0: ragdoll, 1: animation")]
    [Tooltip("Multiplies both region sliders. Zero releases the whole body; unconsciousness temporarily overrides all strengths.")]
    [FormerlySerializedAs("animationStrength")]
    [SerializeField, Range(0f, 1f)] private float overallStrength = 1f;
    [Tooltip("Spine, head and arms. Effective strength = Overall × Upper Body.")]
    [SerializeField, Range(0f, 1f)] private float upperBodyStrength = 1f;
    [Tooltip("Pelvis and legs. Effective strength = Overall × Lower Body.")]
    [SerializeField, Range(0f, 1f)] private float lowerBodyStrength = 1f;

    // Stable internal tuning; interaction is controlled by the three strength sliders.
    private const float angularSpring = 600f, angularDamping = 45f;
    private const float pelvisSpring = 180f, pelvisDamping = 25f;
    private const float maxAngularAcceleration = 1200f, maxPelvisAcceleration = 200f;

    private readonly List<Limb> limbs = new List<Limb>();
    private Transform physicsRoot;
    private bool active, hasTarget, originalRootTrigger, animatorWasEnabled;
    private AnimatorCullingMode originalCulling;
    private bool hasVisiblePose;

    public float OverallStrength { get => overallStrength; set => overallStrength = Mathf.Clamp01(value); }
    public float UpperBodyStrength { get => upperBodyStrength; set => upperBodyStrength = Mathf.Clamp01(value); }
    public float LowerBodyStrength { get => lowerBodyStrength; set => lowerBodyStrength = Mathf.Clamp01(value); }
    public void SetOverallStrength(float value) => OverallStrength = value;
    public void SetUpperBodyStrength(float value) => UpperBodyStrength = value;
    public void SetLowerBodyStrength(float value) => LowerBodyStrength = value;
    // Preserve existing UI callbacks/API from the original single-slider version.
    public float AnimationStrength { get => OverallStrength; set => OverallStrength = value; }
    public void SetAnimationStrength(float value) => OverallStrength = value;

    private void OnEnable()
    {
        if (!physicsRoot && !InitializeRig()) { enabled = false; return; }
        animatorWasEnabled = animator.enabled;
        originalCulling = animator.cullingMode;
        animator.enabled = true;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        if (rootCollider)
        {
            originalRootTrigger = rootCollider.isTrigger;
            rootCollider.isTrigger = true;
        }
        physicsRoot.gameObject.SetActive(true);
        hasTarget = false;
        CaptureAnimation();
        foreach (var limb in limbs)
        {
            limb.impactRemaining = 0f;
            limb.impactWeight = limb.impactVelocity = 0f;
            limb.body.position = limb.position;
            limb.body.rotation = limb.rotation;
        }
        active = true;
        CacheVisiblePose();
        BindRecovery();
    }

    private float Strength(Limb limb)
    {
        float strength = recoveryStrength * Mathf.Clamp01(overallStrength) * Mathf.Clamp01(limb.lowerBody ? lowerBodyStrength : upperBodyStrength);
        if (strength >= 1f) return 1f;
        return strength * (1f - limb.impactSoftness * limb.impactWeight);
    }

    private void UpdateImpact(Limb limb)
    {
        float target = Mathf.Clamp01(limb.impactRemaining / Mathf.Max(0.01f, limb.impactRecoveryTime));
        limb.impactWeight = Mathf.SmoothDamp(limb.impactWeight, target, ref limb.impactVelocity,
            Mathf.Max(0.01f, limb.impactResponseTime), Mathf.Infinity, Time.fixedDeltaTime);
        limb.impactRemaining = Mathf.Max(0f, limb.impactRemaining - Time.fixedDeltaTime);
    }

    // Remove the previous rendered physics pose before the Animator samples this frame.
    private void Update() { if (active) RestoreAnimation(); }

    private void FixedUpdate()
    {
        if (!active) return;
        UpdateRecoveryStrength();
        foreach (var limb in limbs)
        {
            UpdateImpact(limb);
            float strength = Strength(limb);
            SetDynamic(limb, strength < 1f);
            if (limb.body.isKinematic)
            {
                limb.body.MovePosition(limb.position);
                limb.body.MoveRotation(limb.rotation);
                continue;
            }
            if (strength <= 0f) continue;
            Vector3 torque = Track(RotationVector(limb.rotation * Quaternion.Inverse(limb.body.rotation)),
                limb.targetAngularVelocity - limb.body.angularVelocity, angularSpring, angularDamping, strength);
            limb.body.AddTorque(Vector3.ClampMagnitude(torque, Mathf.Max(0f, maxAngularAcceleration)), ForceMode.Acceleration);
            limb.body.AddForce(-Physics.gravity * strength, ForceMode.Acceleration);
            if (limb.parent != null) continue;
            Vector3 support = Track(limb.position - limb.body.position, limb.targetVelocity - limb.body.linearVelocity,
                pelvisSpring, pelvisDamping, strength);
            limb.body.AddForce(Vector3.ClampMagnitude(support, Mathf.Max(0f, maxPelvisAcceleration)), ForceMode.Acceleration);
        }
    }

    private void SetDynamic(Limb limb, bool dynamic)
    {
        if (limb.body.isKinematic == !dynamic) return;
        if (dynamic)
        {
            limb.body.position = limb.position;
            limb.body.rotation = limb.rotation;
            limb.body.isKinematic = false;
            limb.body.linearVelocity = limb.targetVelocity;
            limb.body.angularVelocity = limb.targetAngularVelocity;
            if (limb.joint) ConnectLimb(limb);
        }
        else
        {
            limb.body.linearVelocity = Vector3.zero;
            limb.body.angularVelocity = Vector3.zero;
            limb.body.isKinematic = true;
        }
        limb.body.useGravity = dynamic;
    }

    private static Vector3 Track(Vector3 error, Vector3 velocityError, float spring, float damping, float strength)
    {
        float dt = Time.fixedDeltaTime;
        float k = Mathf.Max(0f, spring) * strength;
        float d = Mathf.Max(Mathf.Max(0f, damping) * strength, 2f * Mathf.Sqrt(k));
        return (error * k + velocityError * (d + k * dt)) / (1f + d * dt + k * dt * dt);
    }

    private void LateUpdate()
    {
        if (!active) return;
        CaptureAnimation();
        foreach (var limb in limbs)
        {
            if (limb.body.isKinematic)
            {
                limb.body.position = limb.position;
                limb.body.rotation = limb.rotation;
            }
            limb.bone.SetPositionAndRotation(limb.body.position, limb.body.rotation);
        }
        // Capture every local pose first; modifying parents earlier would distort children.
        foreach (var limb in limbs)
        {
            limb.physicsLocalPosition = limb.bone.localPosition;
            limb.physicsLocalRotation = limb.bone.localRotation;
        }
        foreach (var limb in limbs)
        {
            float strength = Strength(limb);
            limb.bone.localPosition = Vector3.Lerp(limb.physicsLocalPosition, limb.localPosition, strength);
            limb.bone.localRotation = Quaternion.Slerp(limb.physicsLocalRotation, limb.localRotation, strength);
        }
        CacheVisiblePose();
    }

    private void CacheVisiblePose()
    {
        // Cache only after every parent/child has been blended. Update restores the
        // animation targets, so a shot during Update must use this rendered pose.
        foreach (var limb in limbs)
            limb.visiblePose = new Pose(limb.bone.position, limb.bone.rotation);
        hasVisiblePose = true;
    }

    private void MatchBodiesToVisiblePose()
    {
        if (!hasVisiblePose) CacheVisiblePose();
        // Align the connected rig together; moving only the struck body would
        // stretch its joints and create an unwanted correction impulse.
        foreach (var limb in limbs)
        {
            limb.body.transform.SetPositionAndRotation(limb.visiblePose.position, limb.visiblePose.rotation);
            limb.body.position = limb.visiblePose.position;
            limb.body.rotation = limb.visiblePose.rotation;
        }
    }

    private void CaptureAnimation()
    {
        foreach (var limb in limbs)
        {
            limb.targetVelocity = hasTarget && Time.deltaTime > 0f ? (limb.bone.position - limb.position) / Time.deltaTime : Vector3.zero;
            limb.targetAngularVelocity = hasTarget && Time.deltaTime > 0f
                ? RotationVector(limb.bone.rotation * Quaternion.Inverse(limb.rotation)) / Time.deltaTime : Vector3.zero;
            limb.position = limb.bone.position;
            limb.rotation = limb.bone.rotation;
            limb.localPosition = limb.bone.localPosition;
            limb.localRotation = limb.bone.localRotation;
        }
        hasTarget = true;
    }

    private void RestoreAnimation()
    {
        foreach (var limb in limbs)
        {
            limb.bone.localPosition = limb.localPosition;
            limb.bone.localRotation = limb.localRotation;
        }
    }

    private static Vector3 RotationVector(Quaternion rotation)
    {
        if (rotation.w < 0f) rotation = new Quaternion(-rotation.x, -rotation.y, -rotation.z, -rotation.w);
        rotation.ToAngleAxis(out float angle, out Vector3 axis);
        return angle > 0.001f && axis.sqrMagnitude > 0.001f && !float.IsNaN(axis.x)
            ? axis.normalized * (angle * Mathf.Deg2Rad) : Vector3.zero;
    }

    /// <summary>Applies a world-space impulse at contact. Returns false for foreign or fully animated limbs.</summary>
    public bool ApplyImpulse(Collider hitCollider, Vector3 point, Vector3 impulse,
        float responseTime = 0.06f, float recoveryTime = 0.35f, float softness = 0.75f)
    {
        if (!active || impulse.sqrMagnitude <= 0f) return false;
        foreach (var limb in limbs)
        {
            if (limb.collider != hitCollider || Strength(limb) >= 1f) continue;
            // Keep the displayed pose continuous at contact. Yield gradually on
            // this limb only; joints transmit momentum without releasing the torso.
            limb.impactResponseTime = Mathf.Max(0.01f, responseTime);
            limb.impactRecoveryTime = Mathf.Max(0.01f, recoveryTime);
            limb.impactSoftness = Mathf.Clamp01(softness);
            limb.impactRemaining = limb.impactRecoveryTime;
            SetDynamic(limb, true);
            MatchBodiesToVisiblePose();
            limb.body.WakeUp();
            limb.body.AddForceAtPosition(impulse, point, ForceMode.Impulse);
            return true;
        }
        return false;
    }

    private void OnDisable()
    {
        UnbindRecovery();
        if (!active) return;
        RestoreAnimation();
        foreach (var limb in limbs) SetDynamic(limb, false);
        if (rootCollider) rootCollider.isTrigger = originalRootTrigger;
        if (animator)
        {
            animator.enabled = animatorWasEnabled;
            animator.cullingMode = originalCulling;
        }
        physicsRoot.gameObject.SetActive(false);
        active = false;
    }

    private void OnDestroy() { if (physicsRoot) Destroy(physicsRoot.gameObject); }
}
