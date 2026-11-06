using UnityEngine;

public class ClimbUp : ActionInstance
{
    private PlayerController controller;
    private Sequence positionSequence;

    public ClimbUp(PlayerController controller, ActionStats actionStats)
    {
        this.controller = controller;
        this.actionStats = actionStats;

        InitializeStates();
    }

    public override void Preparing()
    {
        controller.moveDirection = Vector3.zero;
        controller.col.enabled = false;
        Vector3 climbPosition;

        // if (controller.stateVariant) 
        // {
        //     climbPosition = controller.checkHit.point + Vector3.up * 1;
        // }
        // else 
        {
            climbPosition = controller.checkHit.point + Vector3.up * 0.75f + controller.transform.forward * 0.1f;
        }

        positionSequence = new Sequence();
        positionSequence.AppendInterval(0.2f).AppendCallback(() =>
        {
            controller.gameObject.InterpolatePosition(
                climbPosition,
                0.2f
            );
        });
        controller.StartCoroutine(positionSequence.ExecuteActions());
    }

    public override void Executing()
    {
    }

    public override void Complete()
    {
        controller.State = controller.movingState;
        controller.col.enabled = true;
        controller.rb.linearVelocity = controller.transform.forward * 2;
    }

    public override void Cancel()
    {
        positionSequence?.Cancel();
        positionSequence = null;
    }

}
