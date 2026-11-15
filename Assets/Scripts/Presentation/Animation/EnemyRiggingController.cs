using UnityEngine.Animations.Rigging;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyRiggingController : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Transform aimTarget;

    [Header("Rigs")]
    [SerializeField] private Rig aimRig;

    [Header("Pursuit Aim")]
    [SerializeField, Min(0f)] private float targetHeight = 1.5f;
    [SerializeField, Min(0f)] private float blendSpeed = 8f;

    [SerializeField] private EnemyController enemyController;

    private Interpolate aimTween;
    private bool trackingTarget;

    private void OnEnable()
    {
        enemyController.pursuingState.OnEnterState += UpdatePursuitAim;
        enemyController.pursuingState.OnFixedUpdate += UpdatePursuitAim;
        enemyController.pursuingState.OnExitState += StopPursuitAim;

        if (enemyController.State == enemyController.pursuingState)
            UpdatePursuitAim();
    }

    private void OnDisable()
    {
        enemyController.pursuingState.OnEnterState -= UpdatePursuitAim;
        enemyController.pursuingState.OnFixedUpdate -= UpdatePursuitAim;
        enemyController.pursuingState.OnExitState -= StopPursuitAim;

        if (aimTween) aimTween.Cancel();
        aimTween = null;
        trackingTarget = false;
        aimRig.weight = 0f;
    }

    private void UpdatePursuitAim()
    {
        Controller target = enemyController.Target;
        if (!target || !target.isActiveAndEnabled)
        {
            StopPursuitAim();
            return;
        }

        aimTarget.position = target.transform.position + Vector3.up * targetHeight;
        if (trackingTarget) return;

        trackingTarget = true;
        SetRigWeight(1f);
    }

    private void StopPursuitAim()
    {
        if (!trackingTarget) return;

        trackingTarget = false;
        SetRigWeight(0f);
    }

    private void SetRigWeight(float weight)
    {
        if (aimTween) aimTween.Cancel();
        aimTween = null;

        float weightDelta = Mathf.Abs(weight - aimRig.weight);
        if (blendSpeed <= 0f || weightDelta == 0f) return;

        aimTween = gameObject.ToFloat(() => aimRig.weight, value => aimRig.weight = value,
            weight, weightDelta / blendSpeed).SetEase(Ease.Linear);
    }
}
