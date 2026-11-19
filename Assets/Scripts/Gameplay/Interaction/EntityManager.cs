using System;
using UnityEngine;

public class EntityManager : MonoBehaviour
{
    public event Action<Controller, Controller, InteractionContext> OnInteraction;
    public Action<Controller, Controller> OnControllerDied;
    public Action<Controller> OnControllerLeveledUp;

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
                OnInteraction?.Invoke(source, target, result);
                return true;

            case InteractionType.Heal:
                target.entity.AddHealth(context.Value);
                result = context;
                OnInteraction?.Invoke(source, target, result);
                return true;

            default:
                return false;
        }
    }

    public void AddEquipment(Controller controller, Equipment equipment)
    {
        if (controller == null || controller.entity == null || equipment == null)
        {
            return;
        }

        controller.entity.AddStatModifier(equipment);
    }

    public bool AddBuff(Controller controller, BuffDefinition buff)
    {
        return controller != null && controller.AddBuff(buff);
    }

    private DamageResult CalculateDamage(Entity source)
    {
        float chance = source.stats.CriticalChance;
        bool isCritical = chance >= 100f ||
            (chance > 0f && UnityEngine.Random.Range(0f, 100f) < chance);

        return new DamageResult
        {
            Damage = source.stats.GetDamage(isCritical),
            IsCritical = isCritical
        };
    }

    private DamageResult Attack(Controller source, Controller target)
    {
        DamageResult result = CalculateDamage(source.entity);
        float mitigated = ApplyDamage(source, target, result.Damage);
        return new DamageResult { Damage = mitigated, IsCritical = result.IsCritical };
    }

    //needs fixing, dies multiple times when attacked with health 0
    private float ApplyDamage(Controller source, Controller target, float amount)
    {
        float mitigated = target.entity.stats.MitigateDamage(amount);
        float healthResult = target.entity.AddHealth(-mitigated);

        if (healthResult <= 0f)
        {
            ControllerDied(source, target);
        }

        return mitigated;
    }

    private void ControllerDied(Controller source, Controller target)
    {
        OnControllerDied?.Invoke(source, target);
        if (source.entity.AddExperience(target.entity.Level))
        {
            OnControllerLeveledUp?.Invoke(source);
        }
    }
}
