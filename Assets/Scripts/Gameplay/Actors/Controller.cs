using System;
using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

public abstract class Controller : MonoBehaviour
{
    public event Action<Weapon, Vector3> OnShoot;
    public event Action<Buff> OnBuffStarted;
    public event Action<Buff> OnBuffFinished;
    private readonly Dictionary<Buff, Sequence> buffs = new();

    public Action<ActionInstance> OnActionInstancePerformed;
    public Action<float, ActionInstance> OnPreparingActionInstance;
    public Action<float, ActionInstance> OnExecutingActionInstance;
    public Action<ActionInstance> OnCompleteActionInstance;
    public Action<ActionInstance> OnCancelActionInstance;
    public Action<State> OnStateChanged;
    public Action<Controller> OnTargetChanged;
    public Action<Controller> OnControllerDied;
    public Action<bool> OnGroundedChanged;
    public Action<bool> OnStateVariationChanged;

    [FormerlySerializedAs("levelProgression")]
    [SerializeField] private StatProfile controllerStats;

    private State state;
    private Controller target;

    [Header("Debug")]
    [SerializeField] private string currentStateName;

    public State State
    {
        get
        {
            return state;
        }
        set
        {
            if (state == value)
            {
                return;
            }

            if (state != null && state.OnExitState != null)
            {
                state.OnExitState();
            }

            state = value;
            currentStateName = !string.IsNullOrEmpty(state.Name) ? state.Name : state.Type.ToString();

            state.OnEnterState?.Invoke();

            OnStateChanged?.Invoke(state);
        }
    }
    public Controller Target
    {
        get
        {
            return target;
        }
        set
        {
            target = value;

            OnTargetChanged?.Invoke(target);
        }
    }

    public Collider col;
    public Rigidbody rb;
    [SerializeField] protected CombatHandler weaponHandler;

    public Entity entity;
    protected virtual float BaseMovementSpeed => 2.5f;
    public List<ActionInstance> ActionInstances { get; set; }
    public ActionInstance CurrentActionInstance { get; private set; }
    public Sequence ActionSequence;
    
    protected virtual void Awake()
    {
        weaponHandler = GetComponent<CombatHandler>();
        ActionInstances = new List<ActionInstance>();

        if (entity == null)
        {
            if (controllerStats == null)
                throw new InvalidOperationException(
                    $"{name} requires a ControllerStats asset. Assign Controller Stats on its controller component.");

            entity = new Entity(controllerStats: controllerStats);
        }
        SetActions();

    }

    public virtual void Start()
    {
        col = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();

        SetOnEnterState();
        SetOnExitState();
        SetOnUpdateState();
        SetOnFixedUpdateState();
    }

    public abstract void SetOnEnterState();

    public abstract void SetOnExitState();

    public abstract void SetOnUpdateState();

    public abstract void SetOnFixedUpdateState();

    public abstract void SetActions();

    public void Shoot(Vector3 aimPoint)
    {
        if (weaponHandler != null && weaponHandler.TryExecuteAttack(this, aimPoint))
        {
            OnShoot?.Invoke(weaponHandler.EquippedWeapon, aimPoint);
        }
    }

    public void AttackController(Controller target)
    {
        Interact(target, new InteractionContext { Type = InteractionType.Attack });
    }

    public void AttackController(Controller target, InteractionContext context)
    {
        if (context == null) return;
        context.Type = InteractionType.Attack;
        Interact(target, context);
    }

    private void Interact(Controller other, InteractionContext ctx)
    {
        if (!EntityManager.Instance.TryProcessInteraction(this, other, ctx, out _))
        {
            return;
        }
    }

    public bool AddBuff(BuffDefinition definition)
    {
        if (definition == null || !isActiveAndEnabled || entity == null ||
            entity.CurrentHealth <= 0f) return false;
        foreach (Buff activeBuff in buffs.Keys)
            if (activeBuff.Definition == definition) return false;

        Buff buff = new Buff(definition);
        Sequence expiry = new Sequence()
            .AppendInterval(buff.Duration)
            .AppendCallback(() => RemoveBuff(buff));

        buffs.Add(buff, expiry);
        entity.AddStatModifier(buff);
        OnBuffStarted?.Invoke(buff);

        // A start listener can remove or replace this activation.
        if (buffs.TryGetValue(buff, out Sequence current) && current == expiry)
            StartCoroutine(expiry.ExecuteActions());

        return true;
    }

    public void RemoveBuff(Buff buff)
    {
        if (ReferenceEquals(buff, null) || !buffs.Remove(buff, out Sequence expiry)) return;

        expiry.Cancel();
        entity.RemoveStatModifier(buff);
        OnBuffFinished?.Invoke(buff);
    }

    public void ClearBuffs()
    {
        if (buffs.Count == 0) return;

        foreach (var activation in new List<KeyValuePair<Buff, Sequence>>(buffs))
        {
            if (buffs.TryGetValue(activation.Key, out Sequence current) && current == activation.Value)
                RemoveBuff(activation.Key);
        }
    }

    public virtual void OnDied()
    {
        ClearBuffs();
        OnControllerDied?.Invoke(this);

        CancelAction();
    }

    protected void PerformAction(ActionInstance action)
    {
        CancelAction();
        CurrentActionInstance = action;
        Sequence actionSequence = new();
        ActionSequence = actionSequence;
        OnActionInstancePerformed?.Invoke(action);
        State = action.preparing;
        action.Preparing();
        OnPreparingActionInstance?.Invoke(action.actionStats.preparingTime, action);

        actionSequence.AppendInterval(action.actionStats.preparingTime).AppendCallback(() =>
        {
            State = action.executing;
            action.Executing();
            OnExecutingActionInstance?.Invoke(action.actionStats.executingTime, action);
        });
        actionSequence.AppendInterval(action.actionStats.executingTime).AppendCallback(CompleteAction);
        StartCoroutine(actionSequence.ExecuteActions());
    } 

    public void CompleteAction()
    {
        if (CurrentActionInstance == null) return;
        ActionInstance action = CurrentActionInstance;
        CurrentActionInstance = null;
        ActionSequence?.Cancel();
        ActionSequence = null;
        action.Complete();
        OnCompleteActionInstance?.Invoke(action);
    }

    public void CancelAction()
    {
        if (CurrentActionInstance == null) return;

        ActionInstance action = CurrentActionInstance;
        CurrentActionInstance = null;
        ActionSequence?.Cancel();
        ActionSequence = null;
        gameObject.CancelInterpolations();
        action.Cancel();
        OnCancelActionInstance?.Invoke(action);
    }

    protected virtual void OnEnable()
    {
        if (weaponHandler != null) weaponHandler.OnAttackConfirmed += AttackController;
        if (entity != null)
        {
            entity.OnEntityDied += OnDied;
        }
    }

    protected virtual void OnDisable()
    {
        ClearBuffs();
        if (weaponHandler != null) weaponHandler.OnAttackConfirmed -= AttackController;
        CancelAction();
        if (entity != null)
        {
            entity.OnEntityDied -= OnDied;
        }
    }
}
