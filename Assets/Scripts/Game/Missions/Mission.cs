using System;
using UnityEngine;

public abstract class Mission : MonoBehaviour
{
    public Action<MissionState> OnStateChanged;

    public MissionResult Result { get; private set; }
    public DateTimeOffset? StartTimestamp { get; internal set; }
    public DateTimeOffset? EndTimestamp { get; internal set; }
    protected virtual float ElapsedTime => 0f;

    [SerializeField] private string currentStateName;

    private MissionState state;
    public MissionState State
    {
        get
        {
            return state;
        }
        set
        {
            if (state == value) return;

            state?.OnExitState?.Invoke();
            state = value;
            currentStateName = state == null ? string.Empty : state.Name;
            state?.OnEnterState?.Invoke();

            OnStateChanged?.Invoke(state);

        }
    }
    public MissionState ReadyState = new MissionState("Ready");
    public MissionState RunningState = new MissionState("Running");
    public MissionState CompletedState = new MissionState("Completed");
    public MissionState FailedState = new MissionState("Failed");
    public MissionState AbortedState = new MissionState("Aborted");

    protected EnemyManager enemyManager;

    protected virtual void Awake()
    {
        enemyManager = gameObject.AddComponent<EnemyManager>();

        ResetMission();
    }

    public virtual void ResetMission()
    {
        if (State != RunningState)
        {
            Result = null;
            State = ReadyState;
        }
    }

    public virtual void StartMission()
    {
        if (GameManager.Instance == null || GameManager.Instance.GameState != GameState.Playing)
        {
            Debug.LogWarning("Cannot start a mission unless the game is playing.", this);
            return;
        }

        if (State == ReadyState)
        {
            State = RunningState;
        }
    }

    public void CompleteMission()
    {
        CompleteMission(new MissionResult("Completed", ElapsedTime, 0, 0, success: true));
    }

    public virtual void CompleteMission(MissionResult result)
    {
        if (State == RunningState)
        {
            Result = result;
            State = CompletedState;
        }
    }

    public virtual void FailMission()
    {
        if (State == RunningState)
        {
            Result = new MissionResult("Failed", ElapsedTime, 0, 0);
            State = FailedState;
        }
    }

    public virtual void AbortMission()
    {
        if (State == RunningState)
        {
            Result = new MissionResult("Aborted", ElapsedTime, 0, 0);
            State = AbortedState;
        }
    }

    protected virtual void Update() => State?.OnUpdate?.Invoke();
    protected virtual void FixedUpdate() => State?.OnFixedUpdate?.Invoke();
}
