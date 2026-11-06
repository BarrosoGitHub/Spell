public abstract class ActionInstance
{
    public State preparing;
    public State executing;

    public ActionStats actionStats;

    protected void InitializeStates()
    {
        preparing = new State(StateType.PREPARING, "Preparing");
        executing = new State(StateType.EXECUTING, "Executing");
    }

    public abstract void Preparing();
    public abstract void Executing();
    public abstract void Complete();
    public abstract void Cancel();
}
