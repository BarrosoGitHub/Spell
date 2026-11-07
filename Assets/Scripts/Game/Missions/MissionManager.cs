using System;
using System.Linq;
using UnityEngine;

public sealed class MissionManager : MonoBehaviour
{
    public Action OnMissionStarted;
    public Action OnMissionCompleted;

    public Type[] AvailableMissions { get; } = typeof(Mission).Assembly.GetTypes()
        .Where(type => type.IsSubclassOf(typeof(Mission)) && !type.IsAbstract && !type.ContainsGenericParameters)
        .OrderBy(type => type.FullName, StringComparer.Ordinal)
        .ToArray();

    public Mission selectedMission { get; private set; }
    public MissionResult LastResult { get; private set; }

    private PlayerController subscribedPlayer;

    public bool SelectMission(int index)
    {
        if (index < 0 || index >= AvailableMissions.Length) return false;
        return SelectMission(AvailableMissions[index]);
    }

    public bool SelectMission(Type missionType)
    {
        if (!Application.isPlaying || Array.IndexOf(AvailableMissions, missionType) < 0)
            return false;
        if (selectedMission != null && selectedMission.GetType() == missionType)
            return true;
        if (selectedMission != null && selectedMission.State == selectedMission.RunningState)
            return false;

        UnsubscribeFromPlayer();
        if (selectedMission != null)
        {
            selectedMission.OnStateChanged -= HandleMissionChangedState;
            selectedMission.gameObject.SetActive(false);
            Destroy(selectedMission.gameObject);
        }

        GameObject missionObject = new GameObject(missionType.Name + " Mission");
        missionObject.transform.SetParent(transform, false);
        selectedMission = (Mission)missionObject.AddComponent(missionType);
        selectedMission.OnStateChanged += HandleMissionChangedState;
        return true;
    }

    public bool StartMission()
    {
        if (selectedMission == null || selectedMission.State == selectedMission.RunningState) return false;
        if (GameManager.Instance == null || GameManager.Instance.GameState != GameState.Playing)
        {
            Debug.LogWarning("Cannot start a mission unless the game is playing.", this);
            return false;
        }

        UnsubscribeFromPlayer();
        subscribedPlayer = GameManager.Instance.playerController;
        if (subscribedPlayer != null)
            subscribedPlayer.OnControllerDied += OnPlayerDied;
        selectedMission.StartMission();

        //This needs better way
        subscribedPlayer.entity.AddCurrentHealth(300, null);
        return true;
    }

    public bool CompleteMission()
    {
        if (selectedMission == null || selectedMission.State != selectedMission.RunningState)
            return false;

        selectedMission.CompleteMission();
        return true;
    }

    public bool FailMission()
    {
        if (selectedMission == null || selectedMission.State != selectedMission.RunningState)
            return false;

        selectedMission.FailMission();
        return true;
    }

    public bool ResetMission()
    {
        if (selectedMission == null || selectedMission.State == selectedMission.RunningState)
            return false;

        selectedMission.ResetMission();
        LastResult = null;
        return true;
    }

    public bool AbortMission()
    {
        if (selectedMission == null || selectedMission.State != selectedMission.RunningState)
            return false;

        selectedMission.AbortMission();
        return true;
    }

    private void HandleMissionChangedState(MissionState state)
    {
        if (selectedMission == null) return;
        if (state != selectedMission.RunningState) UnsubscribeFromPlayer();

        if (state == selectedMission.ReadyState)
        {
            selectedMission.StartTimestamp = null;
            selectedMission.EndTimestamp = null;
        }
        else if (state == selectedMission.RunningState)
        {
            selectedMission.StartTimestamp = DateTimeOffset.UtcNow;
            selectedMission.EndTimestamp = null;
        }
        else if (selectedMission.StartTimestamp.HasValue && !selectedMission.EndTimestamp.HasValue &&
                 (state == selectedMission.CompletedState || state == selectedMission.FailedState ||
                  state == selectedMission.AbortedState))
        {
            selectedMission.EndTimestamp = DateTimeOffset.UtcNow;
        }

        if (state == selectedMission.RunningState)
        {
            LastResult = null;
            OnMissionStarted?.Invoke();
        }
        if (state == selectedMission.CompletedState)
        {
            LastResult = selectedMission.Result;
            OnMissionCompleted?.Invoke();
        }
    }

    private void OnPlayerDied(Controller player)
    {
        if (selectedMission == null || selectedMission.State != selectedMission.RunningState) return;
        FailMission();
    }

    private void UnsubscribeFromPlayer()
    {
        if (subscribedPlayer != null)
            subscribedPlayer.OnControllerDied -= OnPlayerDied;
        subscribedPlayer = null;
    }

    private void OnDestroy()
    {
        UnsubscribeFromPlayer();
        if (selectedMission != null)
            selectedMission.OnStateChanged -= HandleMissionChangedState;
    }
}
