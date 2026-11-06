using UnityEngine;

public sealed class Survival : Mission
{
    [SerializeField] private PlayerController player;

    public float SurvivalTime { get; private set; }
    protected override float ElapsedTime => SurvivalTime;

    protected override void Awake()
    {
        RunningState.OnEnterState += ResetSurvivalTime;
        RunningState.OnUpdate += UpdateSurvival;
        base.Awake();
    }

    private void ResetSurvivalTime() => SurvivalTime = 0f;

    private void Start()
    {
        if (player == null && GameManager.Instance != null)
            player = GameManager.Instance.playerController;
    }

    private void UpdateSurvival()
    {
        if (player == null)
        {
            FailMission();
            return;
        }

        SurvivalTime += Time.deltaTime;
    }
}
