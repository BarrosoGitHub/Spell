using UnityEngine;
using UnityEngine.AI;

public sealed class AttackSummonSpike : ActionInstance
{
    public const float Range = 12f;
    private const float HitRadius = 1.5f;
    private readonly EnemyController controller;
    private readonly BossSpikeEffect effect;
    private Vector3 position;
    private bool valid;

    public AttackSummonSpike(EnemyController controller, BossSpikeEffect effect)
    {
        this.controller = controller;
        this.effect = effect;
        actionStats.preparingTime = 1.2f;
        actionStats.executingTime = 0.9f;
        InitializeStates();
    }

    public override void Preparing()
    {
        var agent = controller.navMeshAgent;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        valid = false;
        if (controller.Target == null || effect == null || !NavMesh.SamplePosition(
                controller.Target.transform.position, out NavMeshHit hit, 3f, NavMesh.AllAreas)) return;
        valid = true;
        position = hit.position;
        effect.ShowWarning(position, HitRadius);
    }

    public override void Executing()
    {
        if (!valid) return;
        effect.Erupt();
        Controller target = controller.Target;
        if (target == null || !target.isActiveAndEnabled || target.entity.CurrentHealth <= 0f) return;
        Vector3 offset = target.transform.position - position;
        float height = offset.y;
        offset.y = 0f;
        if (offset.sqrMagnitude <= HitRadius * HitRadius && Mathf.Abs(height) < 2.5f)
            controller.AttackController(target);
    }

    public override void Complete()
    {
        if (effect != null) effect.Clear();
        valid = false;
        if (controller.navMeshAgent.enabled && controller.navMeshAgent.isOnNavMesh)
            controller.navMeshAgent.isStopped = false;
        controller.State = controller.Target != null ? controller.pursuingState : controller.idlingState;
    }

    public override void Cancel() => Complete();
}
