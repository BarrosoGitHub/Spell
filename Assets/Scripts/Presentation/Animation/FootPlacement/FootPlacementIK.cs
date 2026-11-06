using UnityEngine;

namespace Breakline.Animation
{
    /// <summary>Humanoid foot placement using two ground probes and world-space stance locks.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Animator))]
    public sealed class FootPlacementIK : MonoBehaviour
    {
        [SerializeField] private LayerMask groundLayers = 8;
        [SerializeField, Min(0f)] private float soleOffset = 0.015f;
        [SerializeField, Min(0.05f)] private float probeHeight = 0.45f;
        [SerializeField, Min(0.05f)] private float probeDepth = 0.55f;
        [SerializeField, Range(0f, 80f)] private float maximumSlope = 55f;
        [SerializeField, Min(0.01f)] private float plantHeight = 0.07f;
        [SerializeField, Min(0.02f)] private float releaseHeight = 0.16f;
        [SerializeField, Min(0.05f)] private float maximumLockDistance = 0.3f;
        [SerializeField, Min(0f)] private float releaseUpwardSpeed = 0.3f;
        [SerializeField, Min(0.01f)] private float blendSpeed = 15f;
        [SerializeField, Min(0f)] private float maximumPelvisDrop = 0.18f;
        [SerializeField] private bool lockPlantedFeet = true;

        private sealed class Foot
        {
            public AvatarIKGoal goal;
            public bool planted, sampled, valid;
            public float previousLocalHeight, weight, cooldown;
            public Vector3 target, localPoint;
            public Quaternion rotation, localRotation;
            public Transform surface;
        }
        private readonly Foot left = new Foot { goal = AvatarIKGoal.LeftFoot };
        private readonly Foot right = new Foot { goal = AvatarIKGoal.RightFoot };
        private Animator animator;
        private bool allowed;
        private int inputFrame = -1;
        private float pelvisOffset;

        public void SetGroundedMovement(bool active, LayerMask layers)
        {
            allowed = active;
            groundLayers = layers;
            inputFrame = Time.frameCount;
        }

        private void Awake() => animator = GetComponent<Animator>();

        private void OnAnimatorIK(int layer)
        {
            if (layer != 0 || !animator || !animator.isHuman) return;
            if (!allowed || inputFrame != Time.frameCount)
            {
                Clear();
                ApplyWeights(left, 0f);
                ApplyWeights(right, 0f);
                return;
            }
            float dt = Time.deltaTime;
            Probe(left, animator.leftFeetBottomHeight, dt);
            Probe(right, animator.rightFeetBottomHeight, dt);
            // Lower the animated pelvis to avoid overextending the leg on the lower surface.
            float desiredDrop = Mathf.Min(VerticalCorrection(left), VerticalCorrection(right));
            desiredDrop = Mathf.Clamp(desiredDrop, -maximumPelvisDrop, 0f);
            pelvisOffset = Mathf.Lerp(pelvisOffset, desiredDrop, 1f - Mathf.Exp(-blendSpeed * dt));
            animator.bodyPosition += Vector3.up * pelvisOffset;
            Apply(left);
            Apply(right);
        }

        private void Probe(Foot foot, float soleHeight, float dt)
        {
            Vector3 animated = animator.GetIKPosition(foot.goal);
            Quaternion animatedRotation = animator.GetIKRotation(foot.goal);
            float localHeight = transform.InverseTransformPoint(animated).y;
            float upwardSpeed = foot.sampled && dt > 0f
                ? (localHeight - foot.previousLocalHeight) * transform.lossyScale.y / dt : 0f;
            foot.previousLocalHeight = localHeight;
            foot.sampled = true;
            foot.cooldown = Mathf.Max(0f, foot.cooldown - dt);
            foot.valid = Physics.Raycast(animated + Vector3.up * probeHeight, Vector3.down, out RaycastHit hit,
                probeHeight + probeDepth, groundLayers, QueryTriggerInteraction.Ignore) &&
                Vector3.Dot(hit.normal, Vector3.up) >= Mathf.Cos(maximumSlope * Mathf.Deg2Rad);
            if (!foot.valid)
            {
                Release(foot);
                foot.weight = Mathf.MoveTowards(foot.weight, 0f, blendSpeed * dt);
                return;
            }

            float clearance = animated.y - soleHeight - hit.point.y;
            bool lifting = upwardSpeed > releaseUpwardSpeed;
            Vector3 desired = hit.point + hit.normal * (soleHeight + soleOffset);
            Quaternion aligned = Quaternion.FromToRotation(Vector3.up, hit.normal) * animatedRotation;
            if (foot.planted)
            {
                Vector3 contact = foot.surface ? foot.surface.TransformPoint(foot.localPoint) : desired;
                bool stretched = Vector3.ProjectOnPlane(contact - animated, Vector3.up).sqrMagnitude >
                    maximumLockDistance * maximumLockDistance;
                if (!lockPlantedFeet || !foot.surface || foot.surface != hit.collider.transform || lifting ||
                    clearance > Mathf.Max(plantHeight + 0.01f, releaseHeight) || stretched)
                    Release(foot);
            }
            if (lockPlantedFeet && !foot.planted && foot.cooldown <= 0f && !lifting && clearance <= plantHeight)
            {
                foot.planted = true;
                foot.surface = hit.collider.transform;
                foot.localPoint = foot.surface.InverseTransformPoint(desired);
                foot.localRotation = Quaternion.Inverse(foot.surface.rotation) * aligned;
            }
            if (foot.planted)
            {
                desired = foot.surface.TransformPoint(foot.localPoint);
                aligned = foot.surface.rotation * foot.localRotation;
            }
            float targetWeight = 1f - Mathf.InverseLerp(plantHeight, Mathf.Max(plantHeight + 0.01f, releaseHeight), clearance);
            if (lifting && !foot.planted) targetWeight = 0f;
            // Swing feet remain animated; only grounded/near-ground samples receive correction.
            foot.weight = Mathf.MoveTowards(foot.weight, targetWeight, blendSpeed * dt);
            foot.target = desired;
            foot.rotation = aligned;
        }

        private float VerticalCorrection(Foot foot) => foot.valid
            ? (foot.target.y - animator.GetIKPosition(foot.goal).y) * foot.weight : 0f;

        private void Apply(Foot foot)
        {
            ApplyWeights(foot, foot.weight);
            if (foot.weight <= 0f) return;
            animator.SetIKPosition(foot.goal, foot.target);
            animator.SetIKRotation(foot.goal, foot.rotation);
        }
        private void ApplyWeights(Foot foot, float weight)
        {
            animator.SetIKPositionWeight(foot.goal, weight);
            animator.SetIKRotationWeight(foot.goal, weight);
        }
        private static void Release(Foot foot)
        {
            if (foot.planted) foot.cooldown = 0.12f;
            foot.planted = false;
            foot.surface = null;
        }
        private void Clear()
        {
            Release(left); Release(right);
            left.weight = right.weight = pelvisOffset = 0f;
            left.sampled = right.sampled = false;
            left.cooldown = right.cooldown = 0f;
        }
        private void OnDisable() { allowed = false; inputFrame = -1; Clear(); }
    }
}
