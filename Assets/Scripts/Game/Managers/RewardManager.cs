using System;
using System.Collections.Generic;
using UnityEngine;

public class RewardManager : MonoBehaviour
{
    public event Action<Controller, IReadOnlyList<Equipment>> OnRewardsGenerated;
    private readonly Dictionary<Controller, Queue<Equipment[]>> pendingRewards =
        new Dictionary<Controller, Queue<Equipment[]>>();

    [SerializeField] private EntityManager entityManager;
    [SerializeField] private EquipmentManager equipmentManager;
    private const int LevelUpRewardCount = 3;

    private void Awake()
    {
        if (equipmentManager == null)
            equipmentManager = GetComponent<EquipmentManager>();

        if (equipmentManager == null)
        {
            Debug.LogError("RewardManager requires an EquipmentManager reference.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (entityManager == null)
            entityManager = EntityManager.Instance;

        entityManager.OnControllerLeveledUp += HandleControllerLeveledUp;
    }

    private void OnDisable()
    {
        if (entityManager != null)
            entityManager.OnControllerLeveledUp -= HandleControllerLeveledUp;

        pendingRewards.Clear();
    }

    public void SelectReward(Controller controller, Equipment equipment)
    {
        if (controller == null || !pendingRewards.TryGetValue(controller, out var queue))
            return;

        if (Array.IndexOf(queue.Peek(), equipment) < 0)
            return;

        entityManager.AddEquipment(controller, equipment);
        queue.Dequeue();
        if (queue.Count == 0)
            pendingRewards.Remove(controller);
        else
            ShowNextReward(controller);
    }

    private void HandleControllerLeveledUp(Controller controller)
    {
        if (controller == null)
            return;

        Equipment[] rewards = new Equipment[LevelUpRewardCount];
        for (int i = 0; i < rewards.Length; i++)
            rewards[i] = equipmentManager.GetRandomEquipment();

        if (!pendingRewards.TryGetValue(controller, out var queue))
        {
            queue = new Queue<Equipment[]>();
            pendingRewards.Add(controller, queue);
        }

        queue.Enqueue(rewards);
        if (queue.Count == 1)
            ShowNextReward(controller);
    }

    private void ShowNextReward(Controller controller)
    {
        OnRewardsGenerated?.Invoke(controller, pendingRewards[controller].Peek());
    }
}
