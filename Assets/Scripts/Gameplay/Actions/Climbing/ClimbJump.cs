using UnityEngine;

public class ClimbJump : ActionInstance
{
    private PlayerController controller;

    public ClimbJump(PlayerController controller, ActionStats actionStats) 
    {
        this.controller = controller;
        this.actionStats = actionStats;

        InitializeStates();
    }

    public override void Preparing() 
    {
        controller.col.enabled = false;
        controller.rb.linearVelocity = Vector3.zero;

        Transform holdTransform = controller.closestHold.transform;
        Vector3 surfaceNormal = CalculationHelper.GetSurfaceNormalFromHold(controller.closestHold.transform);
        Vector3 movePos = holdTransform.position + Vector3.down * 0.75f + surfaceNormal * 0.3f;
        

        // if (controller.StateVariation)
        // {
        //     movePos = holdTransform.position + Vector3.down * 1.12f;
        // }

        controller.gameObject.InterpolateRotation(Quaternion.LookRotation(-surfaceNormal), actionStats.preparingTime);
        // controller.gameObject.InterpolatePosition(movePos, startUpTime).SetEase(Ease.EaseInOutBack);
        controller.gameObject.InterpolateMoveBy(movePos - controller.transform.position, actionStats.preparingTime).SetEase(Ease.EaseInOutBack);
        controller.gameObject.InterpolateMoveBy(Vector3.up * 0.35f, actionStats.preparingTime / 2).OnComplete(() => controller.gameObject.InterpolateMoveBy(Vector3.down * 0.35f, actionStats.preparingTime /2));
        

//         Vector3 destinationVector = movePos - playerController.transform.position;



    }

    public override void Complete() 
    {
        controller.col.enabled = true;
        controller.State = controller.hangingState;
    }

    public override void Executing()
    {
    }

    public override void Cancel()
    {
        controller.gameObject.CancelInterpolations();
        controller.col.enabled = true;
    }
}
