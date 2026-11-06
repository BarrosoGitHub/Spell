using UnityEngine;

public sealed class Dodge : ActionInstance
{
    private const float Speed = 15f;
    private readonly PlayerController controller;
    private Vector3 direction;

    public Dodge(PlayerController controller, ActionStats actionStats)
    {
        this.controller = controller;
        this.actionStats = actionStats;
        InitializeStates();
    }

    public override void Preparing()
    {
        direction = controller.GetRelativeDirection();
        if (direction == Vector3.zero)
            direction = controller.transform.forward;
    }

    public override void Executing()
    {
        Vector3 velocity = direction * Speed;
        velocity.y = controller.rb.linearVelocity.y;
        controller.rb.linearVelocity = velocity;
    }

    public override void Complete()
    {
        Cancel();
    }

    public override void Cancel()
    {
        Vector3 velocity = controller.rb.linearVelocity;
        controller.rb.linearVelocity = new Vector3(0f, velocity.y, 0f);
        if (controller.State == preparing || controller.State == executing)
            controller.State = controller.movingState;
    }
}
