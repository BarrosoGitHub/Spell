public class GettingUp : ActionInstance
{
    private readonly EnemyController controller;

    public GettingUp(EnemyController controller, float preparingTime, float executingTime)
    {
        this.controller = controller;
        actionStats.preparingTime = preparingTime;
        actionStats.executingTime = executingTime;

        InitializeStates();
    }

    public override void Preparing()
    {
        controller.navMeshAgent.enabled = false;
    }

    public override void Executing()
    {
    }

    public override void Complete()
    {
        controller.State = controller.Target != null
            ? controller.pursuingState
            : controller.idlingState;
    }

    public override void Cancel()
    {
        Complete();
    }
}
