using UnityEngine;

public sealed class GameplayLoggingDebugModule
{
    public bool LogCombat { get; set; }
    public bool LogMissions { get; set; }

    private EntityManager interactions;
    private Mission mission;

    public void Bind(EntityManager entityManager, Mission selectedMission)
    {
        if (interactions != entityManager)
        {
            if (interactions != null) interactions.OnInteraction -= LogInteraction;
            interactions = entityManager;
            if (interactions != null) interactions.OnInteraction += LogInteraction;
        }
        if (mission != selectedMission)
        {
            if (mission != null) mission.OnStateChanged -= LogMission;
            mission = selectedMission;
            if (mission != null) mission.OnStateChanged += LogMission;
        }
    }

    private void LogInteraction(Controller source, Controller target, InteractionContext context)
    {
        if (!LogCombat || context.Type != InteractionType.Attack || source == null || target == null) return;
        Debug.Log($"{source.name} dealt {context.Value:0.##} damage to {target.name}" +
            $"{(context.IsCritical ? " (critical)" : "")}. Health: {target.entity.CurrentHealth:0.##}/{target.entity.stats.MaxHealth:0.##}", target);
    }

    private void LogMission(MissionState state)
    {
        if (LogMissions && mission != null)
            Debug.Log($"{mission.GetType().Name} mission: {state.Name}.", mission);
    }

    public void Clear() => Bind(null, null);
}
