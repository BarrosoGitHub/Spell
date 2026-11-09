using System;
using UnityEngine;

public enum BuffType
{
    Buff,
    Debuff
}

[CreateAssetMenu(fileName = "Buff", menuName = "Gameplay/Buff")]
public sealed class Buff : ScriptableObject
{
    [SerializeField] private BuffType type;
    [Tooltip("Lifetime in scaled seconds. Zero expires immediately.")]
    [SerializeField, Min(0f)] private float duration = 5f;

    [Header("Additive Stat Effects (negative values reduce stats)")]
    [SerializeField] private float maxHealth;
    [SerializeField] private float baseDamage;
    [SerializeField] private float attackSpeed;
    [SerializeField] private float defense;
    [Tooltip("Percentage points: 5 adds 5% critical chance.")]
    [SerializeField] private float criticalChance;
    [Tooltip("Added multiplier: 0.5 adds 0.5x critical damage.")]
    [SerializeField] private float criticalDamage;

    public BuffType Type => type;

    public void Activate(Controller controller)
    {
        if (controller == null || !controller.AddBuff(this)) return;

        Sequence sequence = new Sequence()
            .AppendInterval(duration)
            .AppendCallback(() => controller.RemoveBuff(this));

        Action<Buff> onFinished = null;
        onFinished = finishedBuff =>
        {
            if (finishedBuff != this) return;
            sequence.Cancel();
            controller.OnBuffFinished -= onFinished;
        };
        controller.OnBuffFinished += onFinished;
        controller.StartCoroutine(sequence.ExecuteActions());
    }

    public Stats GetStats()
    {
        return new Stats(
            maxHealth: maxHealth,
            baseDamage: baseDamage,
            attackSpeed: attackSpeed,
            defense: defense,
            criticalChance: criticalChance,
            criticalDamage: criticalDamage);
    }
}
