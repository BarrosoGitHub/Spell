using System;

public sealed class ProgressionEffectsModule : IDisposable
{
    private readonly EffectsManager effects;
    private readonly PlayerController player;
    private readonly EntityManager entities;
    private readonly MissionManager missions;
    private readonly RewardManager rewards;
    private EndlessWaves waves;
    private int lastWave;

    public ProgressionEffectsModule(EffectsManager effects, PlayerController player,
        EntityManager entities, MissionManager missions, RewardManager rewards)
    {
        this.effects = effects;
        this.player = player;
        this.entities = entities;
        this.missions = missions;
        this.rewards = rewards;
        entities.OnControllerLeveledUp += OnLevelUp;
        if (missions)
        {
            missions.OnMissionSelected += OnMissionSelected;
            missions.OnMissionStarted += OnMissionStarted;
            missions.OnMissionCompleted += OnMissionCompleted;
            OnMissionSelected(missions.selectedMission);
        }
        if (rewards) rewards.OnRewardSelected += OnRewardSelected;
    }

    private void OnMissionSelected(Mission mission)
    {
        if (waves)
        {
            waves.OnWaveStarted -= OnWaveStarted;
            waves.OnStateChanged -= OnWaveStateChanged;
        }
        waves = mission as EndlessWaves;
        lastWave = 0;
        if (waves)
        {
            waves.OnWaveStarted += OnWaveStarted;
            waves.OnStateChanged += OnWaveStateChanged;
        }
    }

    private void OnWaveStateChanged(MissionState state)
    {
        if (waves && state == waves.ReadyState) lastWave = 0;
    }

    private void OnMissionStarted()
    {
        // RunningState enters (and spawns wave one) before the manager announces the start.
        if (waves) OnWaveStarted(waves.CurrentWave);
    }

    private void OnWaveStarted(int wave)
    {
        if (wave <= 0 || wave == lastWave) return;
        lastWave = wave;
        effects.Play(wave % 5 == 0 ? GameplayEffect.BossWave : GameplayEffect.Wave, player.transform.position);
    }

    private void OnLevelUp(Controller controller)
    {
        if (controller == player) effects.Play(GameplayEffect.LevelUp, player.transform.position);
    }

    private void OnRewardSelected(Controller controller, Equipment equipment)
    {
        if (controller == player) effects.Play(GameplayEffect.RewardSelected, player.transform.position);
    }

    private void OnMissionCompleted() => effects.Play(GameplayEffect.MissionComplete, player.transform.position);

    public void Dispose()
    {
        if (entities) entities.OnControllerLeveledUp -= OnLevelUp;
        if (waves)
        {
            waves.OnWaveStarted -= OnWaveStarted;
            waves.OnStateChanged -= OnWaveStateChanged;
        }
        if (missions)
        {
            missions.OnMissionSelected -= OnMissionSelected;
            missions.OnMissionStarted -= OnMissionStarted;
            missions.OnMissionCompleted -= OnMissionCompleted;
        }
        if (rewards) rewards.OnRewardSelected -= OnRewardSelected;
    }
}
