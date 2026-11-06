using UnityEngine;

public class Grab : ActionInstance
{
    private PlayerController controller;
    public Grab(PlayerController controller, ActionStats actionStats) 
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
        Vector3 surfaceNormal = CalculationHelper.GetSurfaceNormalFromHold(controller.closestHold.transform); // Calculation this 2 times
        Vector3 movePos = holdTransform.position + Vector3.down * 0.75f + surfaceNormal * 0.3f;

        // if (controller.StateVariation) 
        // {
        //     movePos = holdTransform.position + Vector3.down * 1.12f;
        // }

        controller.transform.rotation = Quaternion.LookRotation(-surfaceNormal);
        controller.gameObject.InterpolatePosition(movePos, actionStats.preparingTime);
    }

    public override void Executing()
    {
        
    }

    public override void Complete()
    {
        controller.State = controller.hangingState;
    }

    public override void Cancel()
    {
    }

}
