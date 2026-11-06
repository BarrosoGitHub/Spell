using System;

public class State
{
    public Action OnEnterState, OnExitState, OnUpdate, OnFixedUpdate;
    public Action<bool> OnStateVariationChanged;
    public StateType Type { get; set; }
    public string Name { get; set; }

    public State(string name = "")
    {
        Type = StateType.CONTINUOUS;
        Name = name;
    }
    public State(StateType stateType, string name = "")
    {
        Type = stateType;
        Name = name;
    }
}

public enum StateType
{
    CONTINUOUS,
    PREPARING,
    EXECUTING
}
