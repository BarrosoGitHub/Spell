using UnityEngine;

public class AttackCircle : ActionInstance
{
    private readonly EnemyController controller;
    private readonly float range;

    public AttackCircle(EnemyController controller, float preparingTime, float executingTime, float range = 4f)
    {
        this.controller = controller; //this needs review im using SO
        this.actionStats.preparingTime = preparingTime;
        this.actionStats.executingTime = executingTime;
        this.range = Mathf.Max(0f, range);

        InitializeStates();
    }

    public override void Preparing()
    {
        controller.navMeshAgent.isStopped = true;
        controller.navMeshAgent.velocity = Vector3.zero;
        controller.navMeshAgent.updateRotation = false;
    }

    public override void Executing()
    {
        if (IsTargetInRange() && controller.Target.entity != null && controller.Target.entity.CurrentHealth > 0f)
            controller.AttackController(controller.Target);
    }

    public override void Complete()
    {
        if (controller.navMeshAgent.enabled && controller.navMeshAgent.isOnNavMesh)
        {
            controller.navMeshAgent.isStopped = false;
            controller.navMeshAgent.updateRotation = true;
        }

        if (controller.Target != null)
        {
            controller.State = controller.pursuingState;
        }
        else
        {
            controller.State = controller.idlingState;
        }
    }

    public override void Cancel()
    {
        Complete();
    }

    private bool IsTargetInRange()
    {
        if (controller == null || controller.Target == null)
        {
            return false;
        }

        Vector3 toTarget = controller.Target.transform.position - controller.transform.position;
        toTarget.y = 0f;
        return toTarget.sqrMagnitude <= range * range;
    }
}
