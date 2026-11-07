using System;
using UnityEngine;

public class EntityManager : MonoBehaviour
{
    public event Action<Controller, Controller, InteractionContext> OnInteraction;

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
                    ? new DamageResult { Damage = ApplyDamage(source, target, Mathf.Max(0f, context.DamageOverride.Value)), IsCritical = false }
                    : Attack(source, target);
                result = new InteractionContext
                {
                    Type = InteractionType.Attack,
                    Value = damageResult.Damage,
                    IsCritical = damageResult.IsCritical
                };
                Debug.Log($"{source.name} dealt {damageResult.Damage:0.##} damage to {target.name}" +
                    $"{(damageResult.IsCritical ? " (critical)" : "")}. Health: {target.entity.CurrentHealth:0.##}/{target.entity.stats.MaxHealth:0.##}", target);
                OnInteraction?.Invoke(source, target, result);
                return true;

            case InteractionType.Heal:
                target.entity.AddCurrentHealth(context.Value, source.entity);
                result = context;
                OnInteraction?.Invoke(source, target, result);
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

    private DamageResult Attack(Controller source, Controller target)
    {
        DamageResult result = CalculateDamage(source.entity);
        float mitigated = ApplyDamage(source, target, result.Damage);
        return new DamageResult { Damage = mitigated, IsCritical = result.IsCritical };
    }

    private float ApplyDamage(Controller source, Controller target, float amount)
    {
        float mitigated = amount - (target.entity.stats.Defense * (1 + target.entity.stats.DefensePerc));
        mitigated = Mathf.Max(0, mitigated);
        target.entity.AddCurrentHealth(-mitigated, source.entity);
        return mitigated;
    }
}
