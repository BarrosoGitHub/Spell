using System;
using UnityEngine;
using System.Collections.Generic;

public abstract class Controller : MonoBehaviour
{
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
    public Action<float> OnConsciousnessChanged;
    public State unconsciousState = new State("Unconscious");

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
    public List<ActionInstance> ActionInstances { get; set; }
    public ActionInstance CurrentActionInstance { get; private set; }
    public Sequence ActionSequence;
    
    [Tooltip("Consciousness recovered per second.")]
    [SerializeField, Min(0f)] private float consciousnessRecovery = 50f;
    private float consciousness = 100f;
    public float Consciousness
    {
        get => consciousness;
        set
        {
            float clamped = Mathf.Clamp(value, 0f, 100f);
            if (consciousness == clamped) return;
            consciousness = clamped;

            UpdateConsciousnessState();

            OnConsciousnessChanged?.Invoke(consciousness);
        }
    }

    protected virtual void UpdateConsciousnessState()
    {
        if (consciousness != 0f || (entity != null && entity.CurrentHealth <= 0f)) return;
        CancelAction();
        State = unconsciousState;
    }

    protected virtual void FixedUpdate()
    {
        Consciousness += consciousnessRecovery * Time.fixedDeltaTime;
    }

    protected virtual void Awake()
    {
        ActionInstances = new List<ActionInstance>();

        //This needs a better way to start
        //ScriptableObject or something
        if (entity == null)
        {
            entity = new Entity(
                new Stats(
                    maxHealth: 5,
                    baseDamage: 1,
                    attackSpeed: 1,
                    defense: 0.76f,
                    criticalChance: 0,
                    criticalDamage: 1
                )
            );
        }
        SetActions();

    }

    public virtual void Start()
    {
        col = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
        weaponHandler = GetComponent<CombatHandler>();

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
        if (!InteractionManager.Instance.TryProcessInteraction(this, other, ctx, out _))
        {
            return;
        }
    }

    public virtual void OnDied()
    {
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
        if (entity != null)
        {
            entity.OnEntityDied += OnDied;
        }
    }

    protected virtual void OnDisable()
    {
        CancelAction();
        if (entity != null)
        {
            entity.OnEntityDied -= OnDied;
        }
    }
}
