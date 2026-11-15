using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Playables;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class AnimatorFrequency : MonoBehaviour
{
    private Animator animator;
    private HitRagdollManager hitRagdoll;
    private RigBuilder rigBuilder;
    private DirectorUpdateMode previousRigUpdateMode;
    private bool rigUpdateModeCaptured;
    private bool previousRagdollEnabled;
    private bool manual;
    private bool previousAnimatorEnabled;
    private bool previousRootMotion;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        hitRagdoll = GetComponent<HitRagdollManager>();
        rigBuilder = GetComponent<RigBuilder>();
    }

    public void SetManualEvaluation(bool value)
    {
        if (manual == value) return;
        manual = value;

        if (value)
        {
            if (hitRagdoll != null)
            {
                previousRagdollEnabled = hitRagdoll.enabled;
                // OnDisable restores the Animator settings owned by the ragdoll.
                hitRagdoll.enabled = false;
            }
            previousAnimatorEnabled = animator.enabled;
            previousRootMotion = animator.applyRootMotion;
            animator.keepAnimatorStateOnDisable = true;
            animator.applyRootMotion = false;
            animator.enabled = false;
            SetRigManual();
        }
        else
        {
            if (rigUpdateModeCaptured && rigBuilder && rigBuilder.graph.IsValid())
                rigBuilder.graph.SetTimeUpdateMode(previousRigUpdateMode);
            rigUpdateModeCaptured = false;
            animator.enabled = previousAnimatorEnabled;
            animator.applyRootMotion = previousRootMotion;
            if (hitRagdoll != null)
                hitRagdoll.enabled = previousRagdollEnabled;
        }
    }

    public void Tick(float elapsedTime)
    {
        if (!manual || !isActiveAndEnabled) return;
        SetRigManual();
        animator.Update(elapsedTime);
        if (rigUpdateModeCaptured && rigBuilder.graph.IsValid())
            rigBuilder.Evaluate(elapsedTime);
    }

    private void SetRigManual()
    {
        if (!rigBuilder || !rigBuilder.graph.IsValid() || rigUpdateModeCaptured) return;

        previousRigUpdateMode = rigBuilder.graph.GetTimeUpdateMode();
        rigBuilder.graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        rigUpdateModeCaptured = true;
    }

    private void OnDisable() => SetManualEvaluation(false);
}
