using System;
using UnityEngine;

public class EntityManager : MonoBehaviour
{
    public Action<Controller, Controller, InteractionContext> OnInteractionProcessed;
    public Action<Controller, Controller, InteractionContext> OnInteractionReceived;

    private static EntityManager instance;

    public static EntityManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject managerObject = new GameObject(nameof(EntityManager));
                instance = managerObject.AddComponent<EntityManager>();
                DontDestroyOnLoad(managerObject);
            }

            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool TryProcessInteraction(Controller source, Controller target, InteractionContext context, out InteractionContext result)
    {
        result = context;

        if (source == null || target == null)
        {
            return false;
        }

        if (source.entity == null || target.entity == null)
        {
            return false;
        }

        switch (context.Type)
        {
            case InteractionType.Attack:
                DamageResult damageResult = context.DamageOverride.HasValue
                    ? new DamageResult { Damage = TakeDamage(target.entity, Mathf.Max(0f, context.DamageOverride.Value)), IsCritical = false }
                    : Attack(source.entity, target.entity);
                result = new InteractionContext
                {
                    Type = InteractionType.Attack,
                    Value = damageResult.Damage,
                    IsCritical = damageResult.IsCritical
                };
                Debug.Log($"{source.name} dealt {damageResult.Damage:0.##} damage to {target.name}" +
                    $"{(damageResult.IsCritical ? " (critical)" : "")}. Health: {target.entity.CurrentHealth:0.##}/{target.entity.stats.MaxHealth:0.##}", target);
                EmitInteractionEvents(source, target, result);
                return true;

            case InteractionType.Heal:
                target.entity.Heal(context.Value);
                result = context;
                EmitInteractionEvents(source, target, result);
                return true;

            default:
                return false;
        }
    }

    private DamageResult CalculateDamage(Entity source)
    {
        float damage = source.stats.BaseDamage * (1 + source.stats.BaseDamagePerc);

        bool isCritical = UnityEngine.Random.Range(0f, 100f) <= source.stats.CriticalChance;

        if (isCritical)
            damage *= source.stats.CriticalDamage;

        return new DamageResult
        {
            Damage = damage,
            IsCritical = isCritical
        };
    }

    private DamageResult Attack(Entity source, Entity target)
    {
        DamageResult result = CalculateDamage(source);
        float mitigated = TakeDamage(target, result.Damage);
        return new DamageResult { Damage = mitigated, IsCritical = result.IsCritical };
    }

    private float TakeDamage(Entity target, float amount)
    {
        float mitigated = amount - (target.stats.Defense * (1 + target.stats.DefensePerc));
        mitigated = Mathf.Max(0, mitigated);
        return target.TakeDamage(mitigated);
    }

    private void EmitInteractionEvents(Controller source, Controller target, InteractionContext result)
    {
        OnInteractionProcessed?.Invoke(source, target, result);
        OnInteractionReceived?.Invoke(target, source, result);
    }
}
