using System;

public sealed class MissionState
{
    public Action OnEnterState, OnExitState, OnUpdate, OnFixedUpdate;

    public string Name { get; }

    public MissionState(string name = "")
    {
        Name = name;
    }
}
