using UnityEngine;

// Optional gameplay integration. The rig and hit simulation remain in the other files.
public sealed partial class HitRagdollManager
{
    [Header("Unconscious recovery")]
    [SerializeField] private Controller controller;
    [SerializeField, Min(0.01f)] private float recoveryBlendDuration = 0.4f;
    [SerializeField] private string faceUpGetUpState = "Base Layer.Getting up";
    [SerializeField] private string faceDownGetUpState = "Base Layer.Standing Up";
    private float recoveryStrength = 1f;
    private float recoveryDuration, heldAnimatorSpeed;
    private bool recovering, holdingGetUp;
    private int heldGetUpHash;
    private Vector3 hipsLocalForward;
    private ActionInstance getUpAction;
    private Vector3 fallenRootOffset;
    private Pose[] rootMovePoses;
    private void BindRecovery()
    {
        if (!controller && physicsParent) controller = physicsParent.GetComponent<Controller>();
        if (!controller) controller = GetComponentInParent<Controller>();
        var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        hipsLocalForward = hips.InverseTransformDirection(animator.transform.forward);
        if (!controller) return;
        getUpAction = controller.ActionInstances?.Find(action => action is GettingUp);
        controller.OnStateChanged += HandleRecoveryState;
        HandleRecoveryState(controller.State);
    }

    private void HandleRecoveryState(State state)
    {
        if (state == controller.unconsciousState)
        {
            SetRagdollActive(true);
            return;
        }
        if (getUpAction != null && state == getUpAction.preparing)
        {
            if (recoveryStrength >= 1f) return;
            PrepareRecoveryAnimation();
            BeginRecovery(Mathf.Min(recoveryBlendDuration, Mathf.Max(0.01f, getUpAction.actionStats.preparingTime)));
            return;
        }
        ReleaseGetUp(getUpAction != null && state == getUpAction.executing);
        if (recoveryStrength < 1f && !recovering) BeginRecovery(recoveryBlendDuration);
    }

    public void SetRagdollActive(bool ragdoll)
    {
        if (!ragdoll) { BeginRecovery(recoveryBlendDuration); return; }
        ReleaseGetUp(false);
        recovering = false;
        recoveryStrength = 0f;
        // Switch now so a knockout cannot display one last animated/kinematic pose.
        foreach (var limb in limbs) SetDynamic(limb, true);
        if (controller && limbs.Count > 0)
            fallenRootOffset = limbs[0].body.position - controller.transform.position;
    }

    public void BeginRecovery(float duration)
    {
        recoveryDuration = Mathf.Max(0.01f, duration);
        recovering = true;
        hasTarget = false; // Alignment is not an animation velocity/torque impulse.
    }

    private void UpdateRecoveryStrength()
    {
        if (controller && controller.State == controller.unconsciousState && !recovering && limbs.Count > 0)
        {
            // Follow the fallen pelvis during simulation, rather than leaving the
            // gameplay root at the pre-fall location until get-up begins.
            Vector3 offset = limbs[0].body.position - fallenRootOffset - controller.transform.position;
            offset.y = 0f;
            MoveRecoveryRoot(offset);
        }
        if (!recovering) return;
        recoveryStrength = Mathf.MoveTowards(recoveryStrength, 1f, Time.fixedDeltaTime / recoveryDuration);
        if (recoveryStrength >= 1f) recovering = false;
    }

    private void MoveRecoveryRoot(Vector3 offset)
    {
        if (!controller || offset.sqrMagnitude < 0.00000001f) return;
        // Moving an ancestor also moves child Rigidbody transforms. Compensate at
        // the rig container before physics can sync that ancestor movement.
        Vector3 rigPosition = physicsRoot.position;
        Quaternion rigRotation = physicsRoot.rotation;
        if (rootMovePoses == null || rootMovePoses.Length != limbs.Count)
            rootMovePoses = new Pose[limbs.Count];
        for (int i = 0; i < limbs.Count; i++)
            rootMovePoses[i] = new Pose(limbs[i].body.position, limbs[i].body.rotation);
        Vector3 destination = controller.transform.position + offset;
        controller.transform.position = destination;
        if (controller.rb) controller.rb.position = destination;
        if (!animator.transform.IsChildOf(controller.transform)) animator.transform.position += offset;
        physicsRoot.SetPositionAndRotation(rigPosition, rigRotation);
        for (int i = 0; i < limbs.Count; i++)
        {
            // Native physics poses may be newer than their Transform this tick.
            // Preserve both so the next SyncTransforms cannot reapply stale poses.
            Rigidbody body = limbs[i].body;
            Pose pose = rootMovePoses[i];
            body.transform.SetPositionAndRotation(pose.position, pose.rotation);
            body.position = pose.position;
            body.rotation = pose.rotation;
        }
        hasTarget = false;
    }

    private void PrepareRecoveryAnimation()
    {
        var pelvis = limbs[0];
        bool faceDown = Vector3.Dot(pelvis.body.rotation * hipsLocalForward, Vector3.down) > 0f;
        int hash = Animator.StringToHash(faceDown ? faceDownGetUpState : faceUpGetUpState);
        if (!animator.HasState(0, hash)) return;
        ReleaseGetUp(false);
        heldAnimatorSpeed = animator.speed;
        heldGetUpHash = hash;
        holdingGetUp = true;
        animator.speed = 0f;
        animator.SetInteger("ActionVariationIndex", faceDown ? 1 : 0);

        // Preserve the simulated world pose while aligning the animation/controller
        // to the fallen body instead of pulling it back to its pre-fall location.
        var positions = new Vector3[limbs.Count];
        var rotations = new Quaternion[limbs.Count];
        for (int i = 0; i < limbs.Count; i++)
        {
            positions[i] = limbs[i].body.position;
            rotations[i] = limbs[i].body.rotation;
        }
        RestoreAnimation();
        animator.Play(hash, 0, 0f);
        animator.Update(0f);
        Vector3 offset = positions[0] - pelvis.bone.position;
        offset.y = 0f;
        MoveRecoveryRoot(offset);
        CaptureAnimation();
        for (int i = 0; i < limbs.Count; i++)
        {
            limbs[i].body.position = positions[i];
            limbs[i].body.rotation = rotations[i];
        }
        hasTarget = false;
    }

    private void ReleaseGetUp(bool play)
    {
        if (!holdingGetUp) return;
        holdingGetUp = false;
        animator.speed = heldAnimatorSpeed;
        if (play && animator.isActiveAndEnabled) animator.Play(heldGetUpHash, 0, 0f);
    }

    private void UnbindRecovery()
    {
        if (controller)
        {
            controller.OnStateChanged -= HandleRecoveryState;
        }
        ReleaseGetUp(false);
        recoveryStrength = 1f;
        recovering = false;
    }
}
