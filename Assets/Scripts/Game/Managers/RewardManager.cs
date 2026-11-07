using System;
using System.Collections.Generic;
using UnityEngine;

public class RewardManager : MonoBehaviour
{
    public event Action<Controller, IReadOnlyList<Equipment>> OnRewardsGenerated;

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
    }

    private void HandleControllerLeveledUp(Controller controller)
    {
        Equipment[] rewards = new Equipment[LevelUpRewardCount];
        for (int i = 0; i < rewards.Length; i++)
            rewards[i] = equipmentManager.GetRandomEquipment();

        OnRewardsGenerated?.Invoke(controller, rewards);
    }
}
