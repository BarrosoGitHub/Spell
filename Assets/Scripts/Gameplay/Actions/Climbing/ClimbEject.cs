using UnityEngine;

public class ClimbEject : ActionInstance
{
    private PlayerController controller;
    private Vector3 ejectDirection;
    private float ejectForce = 10;
    public ClimbEject(PlayerController controller, ActionStats actionsStats) 
    {
        this.controller = controller;
        this.actionStats = actionsStats;

        InitializeStates();
    }

    public override void Preparing()
    {
        controller.col.enabled = false;
        controller.rb.linearVelocity = Vector3.zero;
        controller.gameObject.InterpolateMoveBy(-controller.transform.forward * 0.5f, actionStats.preparingTime).SetEase(Ease.EaseInBack);

        ejectDirection = controller.GetRelativeDirection();
    }

    public override void Executing()
    {
        controller.rb.AddForce(ejectDirection * ejectForce, ForceMode.Impulse);
        controller.transform.rotation = Quaternion.LookRotation(ejectDirection);
    }

    public override void Complete()
    {
        controller.State = controller.movingState;
        controller.col.enabled = true;
    }

    public override void Cancel()
    {
    }

}
