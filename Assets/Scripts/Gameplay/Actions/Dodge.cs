using UnityEngine;

public sealed class Dodge : ActionInstance
{
    private const float Speed = 15f;
    private readonly PlayerController controller;
    private Vector3 direction;
    private Vector3 savedVelocity;
    private bool savedKinematic;
    private bool tweening;
    private Interpolate movement;

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
        Vector3 start = controller.transform.position;
        Vector3 destination = start + direction.normalized * Speed * actionStats.executingTime;
        savedVelocity = controller.rb.linearVelocity;
        savedKinematic = controller.rb.isKinematic;
        controller.rb.isKinematic = true;
        tweening = true;
        movement = controller.gameObject.AddComponent<Interpolate>();
        movement.Initialize(actionStats.executingTime, progress =>
        {
            Vector3 next = Vector3.Lerp(start, destination, progress);
            Vector3 delta = next - controller.rb.position;
            float distance = delta.magnitude;
            if (distance > 0f && controller.rb.SweepTest(delta / distance, out RaycastHit hit,
                distance, QueryTriggerInteraction.Ignore))
            {
                next = controller.rb.position + delta.normalized * Mathf.Max(0f, hit.distance - 0.02f);
                destination = next;
                start = next;
            }
            controller.rb.position = next;
            controller.transform.position = next;
        });
    }

    public override void Complete()
    {
        Cancel();
    }

    public override void Cancel()
    {
        if (movement) movement.Cancel();
        movement = null;
        if (tweening)
        {
            controller.rb.isKinematic = savedKinematic;
            if (!savedKinematic) controller.rb.linearVelocity = savedVelocity;
            tweening = false;
        }
        if (controller.State == preparing || controller.State == executing)
            controller.State = controller.movingState;
    }
}
