using System;
using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    public Action<Controller, Controller, InteractionContext> OnInteractionProcessed;
    public Action<Controller, Controller, InteractionContext> OnInteractionReceived;

    private static InteractionManager instance;

    public static InteractionManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject managerObject = new GameObject(nameof(InteractionManager));
                instance = managerObject.AddComponent<InteractionManager>();
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
                    ? new DamageResult { Damage = target.entity.TakeDamage(Mathf.Max(0f, context.DamageOverride.Value)), IsCritical = false }
                    : source.entity.Attack(target.entity);
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

    private void EmitInteractionEvents(Controller source, Controller target, InteractionContext result)
    {
        OnInteractionProcessed?.Invoke(source, target, result);
        OnInteractionReceived?.Invoke(target, source, result);
    }
}
