using UnityEngine;

public class AttackDirectional : ActionInstance
{
    private readonly EnemyController controller;
    private readonly float range;
    private readonly float halfAngleDeg;

    public float Range => range;
    public float HalfAngleDeg => halfAngleDeg;

    public AttackDirectional(EnemyController controller, float preparingTime, float executingTime, float range = 3f, float halfAngleDeg = 10f)
    {
        this.controller = controller;
        this.actionStats.preparingTime = preparingTime;
        this.actionStats.executingTime = executingTime;
        this.range = Mathf.Max(0f, range);
        this.halfAngleDeg = Mathf.Clamp(halfAngleDeg, 1f, 89f);

        InitializeStates();
    }

    public override void Preparing()
    {
        controller.navMeshAgent.isStopped = true;
        controller.navMeshAgent.velocity = Vector3.zero;
        controller.navMeshAgent.updateRotation = false;

        if (controller.Target != null)
        {
            Vector3 direction = (controller.Target.transform.position - controller.transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                controller.transform.rotation = Quaternion.LookRotation(direction);
            }
        }
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

        Vector3 localTarget = controller.transform.InverseTransformPoint(controller.Target.transform.position);
        if (localTarget.z < 0f || localTarget.z > range)
        {
            return false;
        }

        float halfWidth = Mathf.Tan(halfAngleDeg * Mathf.Deg2Rad) * range;
        return Mathf.Abs(localTarget.x) <= halfWidth;
    }
}
