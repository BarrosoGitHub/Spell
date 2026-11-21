using UnityEngine;
using UnityEngine.AI;

public sealed class AttackJump : AttackDirectional
{
    public const float DefaultRange = 7f;
    private const float JumpHeight = 1.5f;
    private const float HitRadius = 1f;
    private readonly EnemyController controller;
    private const float PreparationTurnSpeed = 90f;
    private Vector3 start;
    private Vector3 destination;
    private Vector3 groundPosition;
    private float elapsed;
    private bool jumping;
    private bool hitTarget;
    private bool savedUpdatePosition;

    public AttackJump(EnemyController controller, float preparingTime, float executingTime)
        : base(controller, preparingTime, executingTime, DefaultRange, Mathf.Atan(HitRadius / DefaultRange) * Mathf.Rad2Deg)
    {
        this.controller = controller;
        preparing.OnFixedUpdate += UpdatePreparationRotation;
        executing.OnFixedUpdate += UpdateJump;
    }

    public override void Preparing()
    {
        controller.navMeshAgent.isStopped = true;
        controller.navMeshAgent.velocity = Vector3.zero;
        controller.navMeshAgent.updateRotation = false;
        elapsed = 0f;
        hitTarget = false;
    }

    private void UpdatePreparationRotation()
    {
        if (controller.Target == null) return;

        Vector3 direction = controller.Target.transform.position - controller.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f) return;

        controller.transform.rotation = Quaternion.RotateTowards(controller.transform.rotation,
            Quaternion.LookRotation(direction), PreparationTurnSpeed * Time.fixedDeltaTime);
    }

    public override void Executing()
    {
        NavMeshAgent agent = controller.navMeshAgent;
        if (!agent.enabled || !agent.isOnNavMesh) return;

        start = controller.transform.position;
        groundPosition = start;
        destination = start + controller.transform.forward * Range;
        // Keep the committed leap inside the walkable corridor, including arena limits.
        if (agent.Raycast(destination, out NavMeshHit hit))
            destination = hit.position + Vector3.up * agent.baseOffset;
        savedUpdatePosition = agent.updatePosition;
        agent.updatePosition = false;
        jumping = true;
    }

    private void UpdateJump()
    {
        if (!jumping) return;

        Vector3 previous = groundPosition;
        elapsed += Time.fixedDeltaTime;
        float progress = Mathf.Clamp01(elapsed / actionStats.executingTime);
        groundPosition = Vector3.Lerp(start, destination, progress);
        controller.navMeshAgent.nextPosition = groundPosition;
        controller.transform.position = groundPosition + Vector3.up * (4f * JumpHeight * progress * (1f - progress));
        TryHitTarget(previous, groundPosition);
    }

    private void TryHitTarget(Vector3 previous, Vector3 position)
    {
        Controller target = controller.Target;
        if (hitTarget || target == null || target.entity.CurrentHealth <= 0f) return;

        // Sweep the tackle footprint so a target cannot be skipped between physics ticks.
        Vector3 segment = position - previous;
        Vector3 offset = target.transform.position - previous;
        segment.y = 0f;
        offset.y = 0f;
        float progress = segment.sqrMagnitude > 0f
            ? Mathf.Clamp01(Vector3.Dot(offset, segment) / segment.sqrMagnitude) : 0f;
        Vector3 closest = Vector3.Lerp(previous, position, progress);
        if ((offset - segment * progress).sqrMagnitude > HitRadius * HitRadius ||
            Mathf.Abs(target.transform.position.y - closest.y) > 2f) return;

        hitTarget = true;
        controller.AttackController(target);
    }

    public override void Complete()
    {
        FinishJump();
        base.Complete();
    }

    public override void Cancel()
    {
        FinishJump();
        base.Complete();
    }

    private void FinishJump()
    {
        if (!jumping) return;
        jumping = false;
        NavMeshAgent agent = controller.navMeshAgent;
        controller.transform.position = groundPosition;
        if (agent.enabled && agent.isOnNavMesh)
            agent.Warp(groundPosition);
        agent.updatePosition = savedUpdatePosition;
    }
}
