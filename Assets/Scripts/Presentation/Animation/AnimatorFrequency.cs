using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class AnimatorFrequency : MonoBehaviour
{
    private Animator animator;
    private HitRagdollManager hitRagdoll;
    private bool previousRagdollEnabled;
    private bool manual;
    private bool previousAnimatorEnabled;
    private bool previousRootMotion;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        hitRagdoll = GetComponent<HitRagdollManager>();
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
        }
        else
        {
            animator.enabled = previousAnimatorEnabled;
            animator.applyRootMotion = previousRootMotion;
            if (hitRagdoll != null)
                hitRagdoll.enabled = previousRagdollEnabled;
        }
    }

    public void Tick(float elapsedTime)
    {
        if (!manual || !isActiveAndEnabled) return;
        animator.Update(elapsedTime);
    }

    private void OnDisable() => SetManualEvaluation(false);
}
