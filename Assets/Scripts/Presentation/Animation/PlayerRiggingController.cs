using UnityEngine.Animations.Rigging;
using UnityEngine;
using System;

public class PlayerRiggingController : MonoBehaviour
{
    public PlayerActionsSO playerActionsSO;
    [Header("Targets")]
    public Transform AimTarget;
    public Transform LArmTarget;
    public Transform RArmTarget;
    public Transform LLegTarget;
    public Transform RLegTarget;

    [Header("Rigs")]
    [SerializeField] private Rig armsRig;
    [SerializeField] private Rig aimRig;

    [Header("Shooting Aim")]
    [SerializeField, Min(0f)] private float shootingPoseHoldTime = 0.2f;
    [SerializeField, Min(0f)] private float shootingBlendTime = 0.1f;
    [Tooltip("Rotation from the shot direction to the hand's local axes.")]
    [SerializeField] private Vector3 shootingHandRotationOffset = new Vector3(0f, 90f, 0f);

    private Sequence shootingReleaseSequence;
    private Interpolate shootingArmTween, shootingRigTween, shootingLookTween;
    private Interpolate otherArmTween;
    private TwoBoneIKConstraint shootingArm;

    [Header("Constraints")]
    [SerializeField] private MultiAimConstraint HeadConstraint;
    [SerializeField] private MultiAimConstraint ChestConstraint;
    [SerializeField] private TwoBoneIKConstraint LArmBoneIK;
    [SerializeField] private TwoBoneIKConstraint RArmBoneIK;
    [SerializeField] private TwoBoneIKConstraint LLegBoneIK;
    [SerializeField] private TwoBoneIKConstraint RLegBoneIK;

    Animator animator;
  
    public PlayerController playerController;
    
    void Start()
    {
        animator = GetComponent<Animator>();

        SetOnEnterState();
        SetOnUpdateState();
        SetOnStateVariationUpdate();
    }

    private (TwoBoneIKConstraint arm, Transform target) ChooseShootingArm(Vector3 aimPoint)
    {
        // This component shares the animated model's transform and facing.
        Vector3 direction = aimPoint - transform.position;
        return Vector3.Dot(transform.right, direction) > 0f
            ? (RArmBoneIK, RArmTarget)
            : (LArmBoneIK, LArmTarget);
    }

    private void AimShootingArm(Weapon weapon, Vector3 aimPoint)
    {
        var (arm, target) = ChooseShootingArm(aimPoint);
        if (!arm || !target || !arm.data.root) return;
        Vector3 direction = aimPoint - arm.data.root.position;
        if (direction.sqrMagnitude <= Mathf.Epsilon) return;

        target.position = aimPoint;
        if (AimTarget) AimTarget.position = aimPoint;
        Vector3 up = Mathf.Abs(Vector3.Dot(direction.normalized, Vector3.up)) > 0.99f
            ? Vector3.forward : Vector3.up;
        target.rotation = Quaternion.LookRotation(direction, up)
            * Quaternion.Euler(shootingHandRotationOffset);

        if (shootingArmTween) shootingArmTween.Cancel();
        if (shootingRigTween) shootingRigTween.Cancel();
        if (shootingLookTween) shootingLookTween.Cancel();
        
        if (shootingArm != arm)
        {
            if (otherArmTween) otherArmTween.Cancel();
            otherArmTween = SetRigWeight(shootingArm, 0f, shootingBlendTime);
        }
        shootingArm = arm;
        shootingArmTween = SetRigWeight(arm, 1f, 0.05f);
        shootingRigTween = SetRigWeight(armsRig, 1f, 0.05f);
        shootingLookTween = SetRigWeight(aimRig, 1f, 0.05f);
        shootingReleaseSequence?.Cancel();
        shootingReleaseSequence = Delay(() =>
        {
            shootingReleaseSequence = null;
            if (shootingArmTween) shootingArmTween.Cancel();
            if (shootingRigTween) shootingRigTween.Cancel();
            if (shootingLookTween) shootingLookTween.Cancel();
            shootingArmTween = SetRigWeight(arm, 0f, shootingBlendTime);
            shootingRigTween = SetRigWeight(armsRig, 0f, shootingBlendTime);
            shootingLookTween = SetRigWeight(aimRig, 0f, 0.4f);
        }, shootingPoseHoldTime);
    }

    private void SetOnEnterState()
    {
        playerController.movingState.OnEnterState += () =>
        {

            SetRigWeight(LArmBoneIK, 0, 0.1f);
            SetRigWeight(RArmBoneIK, 0, 0.1f);
            SetRigWeight(LLegBoneIK, 0, 0.1f);
            SetRigWeight(RLegBoneIK, 0, 0.1f);

            LArmTarget.SetParent(null);
            RArmTarget.SetParent(null);

        };
        playerController.hangingState.OnEnterState += () =>
        {
            LArmTarget.SetParent(playerController.closestHold.transform);
            RArmTarget.SetParent(playerController.closestHold.transform);
        };

        //Grab
        playerController.ActionInstances[0].preparing.OnEnterState += () =>
        {
            UpdateHandMask(true, 0.1f);
            UpdateHandMask(false, 0.1f);

            UpdateGrabIK(0f);
            SetRigWeight(armsRig, 1f, 0f);

            Delay(() => SetRigWeight(LArmBoneIK, 1, 0.1f), 0.1f);
            Delay(() => SetRigWeight(RArmBoneIK, 1, 0.1f), 0.1f);
            Delay(() => SetRigWeight(LLegBoneIK, 1, 0.1f), 0.025f);
            Delay(() => SetRigWeight(RLegBoneIK, 1, 0.1f), 0.025f);

            // if (!playerController.StateVariation)
            // {
            //     Delay(() => SetRigWeight(LLegBoneIK, 1, 0.17f), 0.1f);
            //     Delay(() => SetRigWeight(RLegBoneIK, 1, 0.175f), 0.15f);
            // }

        };
        //Climb up
        playerController.ActionInstances[1].preparing.OnEnterState += () =>
        {
            float executingTime = playerController.ActionInstances[1].actionStats.executingTime;

            Delay(() => SetRigWeight(LArmBoneIK, 0, 0.2f), executingTime * 0.4f);
            Delay(() => SetRigWeight(RArmBoneIK, 0, 0.2f), executingTime * 0.4f);
            Delay(() => SetRigWeight(LLegBoneIK, 0, 0.1f), executingTime * 0.2f);
            Delay(() => SetRigWeight(RLegBoneIK, 0, 0.1f), executingTime * 0.025f);

        };

        playerController.ActionInstances[2].preparing.OnEnterState += () =>
        {
            // Delay(() => SetRigWeight(LArmBoneIK, 0, 0.1f), 0.3f);
            // Delay(() => SetRigWeight(RArmBoneIK, 0, 0.1f), 0.3f);
            // SetRigWeight(LLegBoneIK, 0, 0.1f);
            // SetRigWeight(RLegBoneIK, 0, 0.1f);
        };
        //Climb Jump
        playerController.ActionInstances[3].preparing.OnEnterState += () =>
        {
            float preparingTime = playerController.ActionInstances[3].actionStats.preparingTime;

            Delay(() => UpdateHandMask(true, preparingTime * 0.125f), preparingTime * 0.475f);
            Delay(() => UpdateHandMask(false, preparingTime * 0.125f), preparingTime * 0.475f);

            Delay(() => SetRigWeight(LArmBoneIK, 0.75f, preparingTime * 0.1f), preparingTime * 0.45f);
            Delay(() => SetRigWeight(RArmBoneIK, 0.75f, preparingTime * 0.1f), preparingTime * 0.45f);
            Delay(() => SetRigWeight(LArmBoneIK, 1, preparingTime * 0.075f), preparingTime * 0.6f);
            Delay(() => SetRigWeight(RArmBoneIK, 1, preparingTime * 0.075f), preparingTime * 0.6f);

            // Delay(() => SetRigWeight(LLegBoneIK, 0.1f, preparingTime * 0.125f), preparingTime * 0.45f);
            // Delay(() => SetRigWeight(RLegBoneIK, 0.1f, preparingTime * 0.125f), preparingTime * 0.45f);

            // // if (!playerController.StateVariation)
            // {
            //     Delay(() => SetRigWeight(RLegBoneIK, 1, preparingTime * 0.175f), preparingTime * 0.6f);
            //     Delay(() => SetRigWeight(LLegBoneIK, 1, preparingTime * 0.175f), preparingTime * 0.6f);
            // }
            // // else
            // {
            //     print("here");
            //     Delay(() => SetRigWeight(LLegBoneIK, 0, preparingTime * 0.175f), preparingTime * 0.65f);
            //     Delay(() => SetRigWeight(RLegBoneIK, 0, preparingTime * 0.175f), preparingTime * 0.65f);
            // }

            Delay(() => UpdateGrabIK(preparingTime * 0.15f), preparingTime * 0.5f);
            SetRigWeight(aimRig, 0.85f, preparingTime * 0.2f);
            Delay(() => SetRigWeight(aimRig, 0, preparingTime * 0.2f), preparingTime * 0.75f);
            AimTarget.gameObject.InterpolatePosition(playerController.closestHold.transform.position + Vector3.up * 0.5f, preparingTime * 0.25f).SetEase(Ease.Linear);
        };
    }

    private void SetOnUpdateState()
    {
        playerController.hangingState.OnFixedUpdate += () =>
        {
            // HangingFootIK();
        };
    }

    private void SetOnStateVariationUpdate()
    {
        playerController.hangingState.OnStateVariationChanged += (value) =>
        {
            if (!value)
            {
                SetRigWeight(RLegBoneIK, 1, 0.175f);
                SetRigWeight(LLegBoneIK, 1, 0.175f);
            }
            else
            {
                SetRigWeight(LLegBoneIK, 0, 0.175f);
                SetRigWeight(RLegBoneIK, 0, 0.175f);
            }
        };
    }

    private void UpdateHandMask(bool left, float blendTime)
    {
        int layer = left ? 1 : 2;
        float weight = 0;

        playerController.gameObject.ToFloat(() => weight, x => weight = x, 0.75f, blendTime)
        .OnUpdate(() =>
        {
            animator.SetLayerWeight(layer, weight);
        })
        .OnComplete(() =>
        {
            playerController.gameObject.ToFloat(() => weight, x => weight = x, 0, blendTime)
            .OnUpdate(() =>
            {
                animator.SetLayerWeight(layer, weight);
            });
        });
    }


    private void UpdateGrabIK(float blendTime)
    {
        Vector3 wallNormal = CalculationHelper.GetSurfaceNormalFromHold(playerController.closestHold.transform);
        Vector3 referenceRight = Vector3.Cross(-wallNormal, playerController.closestHold.transform.forward);

        Vector3 LArmPos = playerController.closestHold.transform.position - Vector3.up * 0.1f - referenceRight * 0.135f;
        Vector3 RArmPos = playerController.closestHold.transform.position - Vector3.up * 0.1f + referenceRight * 0.135f;

        LArmTarget.gameObject.InterpolatePosition(LArmPos, blendTime);
        RArmTarget.gameObject.InterpolatePosition(RArmPos, blendTime);

        LArmTarget.transform.rotation = Quaternion.LookRotation(-wallNormal) * Quaternion.Euler(0, 90, 0);
        RArmTarget.transform.rotation = Quaternion.LookRotation(-wallNormal) * Quaternion.Euler(0, -90, 0);

        Vector3 LLegPos = playerController.closestHold.transform.position + Vector3.down + wallNormal * 0.18f - referenceRight * 0.2f;
        Vector3 RLegPos = playerController.closestHold.transform.position + Vector3.down + wallNormal * 0.18f + referenceRight * 0.2f;
        
        LLegTarget.gameObject.InterpolatePosition(LLegPos, blendTime).SetEase(Ease.Linear);
        RLegTarget.gameObject.InterpolatePosition(RLegPos, blendTime).SetEase(Ease.Linear);

        LLegTarget.rotation = Quaternion.LookRotation(-wallNormal) * Quaternion.Euler(90, 0, 0);
        RLegTarget.rotation = Quaternion.LookRotation(-wallNormal) * Quaternion.Euler(90, 0, 0);

        // Ray refRay = new(playerController.closestHold.transform.position + Vector3.up * 0.05f, playerController.closestHold.transform.forward);

        // if (Physics.Raycast(refRay, out RaycastHit refHit, 1, groundMask))
        // {
        //     Vector3 referencePos = playerController.closestHold.transform.position + refHit.normal * 0.05f + playerController.closestHold.transform.forward * 0.01f;
        //     Vector3 referenceRight = Vector3.Cross(refHit.normal, playerController.closestHold.transform.forward);

        //     // Debug.DrawRay(playerController.closestHold.transform.position, playerController.closestHold.transform.forward * 0.2f, Color.yellow, 10);
        //     // Debug.DrawRay(playerController.closestHold.transform.position, refHit.normal * 0.2f, Color.blue, 10);
        //     // Debug.DrawRay(playerController.closestHold.transform.position, referenceRight * 0.15f, Color.red, 10);
        //     // Debug.DrawRay(playerController.closestHold.transform.position, -referenceRight * 0.15f, Color.green, 10);

        //     Ray LArmRay = new(referencePos - referenceRight * 0.05f, -refHit.normal);
        //     Ray RArmRay = new(referencePos + referenceRight * 0.05f, -refHit.normal);

        //     if (Physics.Raycast(LArmRay, out RaycastHit LArmHit, 1, groundMask))
        //     {
        //         LArmTarget.transform.position = LArmHit.point - refHit.normal * 0.1f - playerController.transform.forward * 0.01f - referenceRight * 0.1f;
        //         LArmTarget.rotation = Quaternion.LookRotation(-wallNormal) * Quaternion.Euler(0, 90, 0);
        //     }
        //     else
        //     {

        //     }

        //     if (Physics.Raycast(RArmRay, out RaycastHit RArmHit, 1, groundMask))
        //     {
        //         RArmTarget.transform.position = RArmHit.point - refHit.normal * 0.1f - playerController.transform.forward * 0.01f + referenceRight * 0.1f;
        //         RArmTarget.rotation = Quaternion.LookRotation(wallNormal) * Quaternion.Euler(0, 90, 0);
        //     }
        // }

        // Vector3 LLegPos = Vector3.zero;
        // Vector3 RLegPos = Vector3.zero;
        // float footSeparation = 0.1f;

        // Vector3 wallParallelVector = Vector3.Cross(wallNormal, Vector3.up);

        // Ray LLegRay = new(playerController.closestHold.transform.position + Vector3.down + wallNormal * 0.1f - wallParallelVector * footSeparation, -wallNormal);
        // Ray RLegRay = new(playerController.closestHold.transform.position + Vector3.down + wallNormal * 0.1f + wallParallelVector * footSeparation, -wallNormal);

        // if (Physics.Raycast(LLegRay, out LLegHit, groundMask))
        // {
        //     LLegPos = new Vector3(LLegHit.point.x, RArmTarget.position.y - 1f, LLegHit.point.z) + wallNormal * 0.18f;
        // }
        // if (Physics.Raycast(RLegRay, out RaycastHit RLegHit, groundMask))
        // {
        //     RLegPos = new Vector3(RLegHit.point.x, LArmTarget.position.y - 1f, RLegHit.point.z) + wallNormal * 0.18f;
        // }

        // LLegTarget.position = LLegPos;
        // RLegTarget.position = RLegPos;

        // LLegTarget.rotation = Quaternion.LookRotation(-wallNormal) * Quaternion.Euler(90, 0, 0);
        // RLegTarget.rotation = Quaternion.LookRotation(-wallNormal) * Quaternion.Euler(90, 0, 0);
    }

    // private void HangingFootIK()
    // {
    //     Vector3 wallNormal = CalculationHelper.GetSurfaceNormalFromHold(playerController.closestHold.transform);
    //     Ray refRay = new(playerController.closestHold.transform.position + Vector3.up * 0.05f, playerController.closestHold.transform.forward);

    //     float footSeparation = 0.1f;

    //     Vector3 wallParallelVector = Vector3.Cross(wallNormal, Vector3.up);

    //     Ray LLegRay = new(playerController.closestHold.transform.position + Vector3.down + wallNormal * 0.1f - wallParallelVector * footSeparation, -wallNormal);
    //     Ray RLegRay = new(playerController.closestHold.transform.position + Vector3.down + wallNormal * 0.1f + wallParallelVector * footSeparation, -wallNormal);

    //     if (Physics.Raycast(LLegRay, out LLegHit, 0.5f, groundMask))
    //     {
    //         LeftFootOnWall = true;
    //     }
    //     else
    //     {
    //         LeftFootOnWall = false;
    //     }
    //     if (Physics.Raycast(RLegRay, out RaycastHit RLegHit, groundMask))
    //     {

    //     }


    //     // LLegTarget.rotation = Quaternion.LookRotation(-wallNormal) * Quaternion.Euler(90, 0, 0);
    //     // RLegTarget.rotation = Quaternion.LookRotation(-wallNormal) * Quaternion.Euler(90, 0, 0);
    // }

    private Interpolate SetRigWeight(Rig rig, float weight, float time)
    {
        if (!rig) return null;
        return playerController.gameObject.ToFloat(() => rig.weight, x => rig.weight = x, weight, time).SetEase(Ease.Linear);
    }

    private Interpolate SetRigWeight(TwoBoneIKConstraint constraint, float weight, float time)
    {
        if (!constraint) return null;
        return playerController.gameObject.ToFloat(() => constraint.weight, x => constraint.weight = x, weight, time).SetEase(Ease.Linear);
    }

    public Sequence Delay(Action action, float amount)
    {
        Sequence sequence = new();
        sequence.AppendInterval(amount).AppendCallback(() => action());
        StartCoroutine(sequence.ExecuteActions());
        return sequence;
    }

    private void OnEnable()
    {
        if (!playerController)
            playerController = GetComponent<PlayerController>();
        if (playerController)
        {
            playerController.OnShoot += AimShootingArm;
        }
    }

    private void OnDisable()
    {
        if (playerController)
        {
            playerController.OnShoot -= AimShootingArm;
        }
        bool wasShooting = shootingReleaseSequence != null || shootingArmTween || shootingRigTween || shootingLookTween || otherArmTween;
        if (shootingArmTween) shootingArmTween.Cancel();
        if (shootingRigTween) shootingRigTween.Cancel();
        if (shootingLookTween) shootingLookTween.Cancel();
        if (otherArmTween) otherArmTween.Cancel();
        shootingArmTween = shootingRigTween = shootingLookTween = otherArmTween = null;
        if (wasShooting)
        {
            shootingReleaseSequence?.Cancel();
            shootingReleaseSequence = null;
            SetRigWeight(LArmBoneIK, 0f, 0f);
            SetRigWeight(RArmBoneIK, 0f, 0f);
            SetRigWeight(armsRig, 0f, 0f);
            SetRigWeight(aimRig, 0f, 0f);
        }
    }
}
