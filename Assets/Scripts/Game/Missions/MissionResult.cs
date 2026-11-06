using System;

[Serializable]
public sealed class MissionResult
{
    public string Outcome { get; }
    public float ElapsedTime { get; }
    public int Score { get; }
    public int Rewards { get; }
    public bool Success { get; }

    public MissionResult(string outcome, float elapsedTime, int score, int rewards, bool success = false)
    {
        Outcome = outcome;
        ElapsedTime = elapsedTime;
        Score = score;
        Rewards = rewards;
        Success = success;
    }
}
