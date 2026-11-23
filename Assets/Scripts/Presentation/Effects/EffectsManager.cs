using System;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using UnityEngine;

public enum GameplayEffect
{
    Shot, Hit, CriticalHit, Kill, PlayerDamage, Dodge, Landing,
    Wave, BossWave, LevelUp, RewardSelected, MissionComplete
}

[DefaultExecutionOrder(100)]
public sealed class EffectsManager : MonoBehaviour
{
    [Serializable]
    private sealed class FeedbackBinding
    {
        public GameplayEffect trigger;
        public bool enabled = true;
        public MMF_Player feedback;
        [Min(0f)] public float cooldown = 0.05f;
        [NonSerialized] public float nextPlayTime;
    }

    [SerializeField] private PlayerController player;
    [SerializeField] private EntityManager entityManager;
    [SerializeField] private MissionManager missionManager;
    [SerializeField] private RewardManager rewardManager;
    [SerializeField] private List<FeedbackBinding> effects = new();

    private PlayerEffectsModule playerEffects;
    private CombatEffectsModule combatEffects;
    private ProgressionEffectsModule progressionEffects;
    private bool initialized;
    private ParticleSystem[] particles;

    private void Start()
    {
        if (!player && GameManager.Instance) player = GameManager.Instance.playerController;
        if (!entityManager) entityManager = FindFirstObjectByType<EntityManager>();
        if (!missionManager) missionManager = FindFirstObjectByType<MissionManager>();
        if (!rewardManager) rewardManager = FindFirstObjectByType<RewardManager>();
        if (!player || player.entity == null || !entityManager)
        {
            Debug.LogError("EffectsManager requires a player with an Entity and an EntityManager.", this);
            enabled = false;
            return;
        }

        foreach (FeedbackBinding binding in effects)
            if (binding.feedback) binding.feedback.Initialization();
        particles = GetComponentsInChildren<ParticleSystem>(true);
        initialized = true;
        Subscribe();
    }

    private void OnEnable()
    {
        if (initialized) Subscribe();
    }

    private void Subscribe()
    {
        playerEffects = new PlayerEffectsModule(this, player);
        combatEffects = new CombatEffectsModule(this, player, entityManager);
        progressionEffects = new ProgressionEffectsModule(this, player, entityManager, missionManager, rewardManager);
    }

    public void Play(GameplayEffect trigger, Vector3 position)
    {
        if (!isActiveAndEnabled || !initialized) return;
        float now = Time.unscaledTime;
        foreach (FeedbackBinding binding in effects)
        {
            if (binding.trigger != trigger || !binding.enabled || !binding.feedback || now < binding.nextPlayTime)
                continue;
            binding.nextPlayTime = now + binding.cooldown;
            binding.feedback.PlayFeedbacks(position);
        }
    }

    private void OnDisable()
    {
        playerEffects?.Dispose();
        combatEffects?.Dispose();
        progressionEffects?.Dispose();
        playerEffects = null;
        combatEffects = null;
        progressionEffects = null;
        foreach (FeedbackBinding binding in effects)
        {
            binding.nextPlayTime = 0f;
            if (!binding.feedback || !initialized) continue;
            binding.feedback.StopFeedbacks();
            binding.feedback.RestoreInitialValues();
        }
        if (particles == null) return;
        foreach (ParticleSystem system in particles)
            if (system) system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
