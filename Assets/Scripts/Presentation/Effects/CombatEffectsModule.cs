using System;

public sealed class CombatEffectsModule : IDisposable
{
    private readonly EffectsManager effects;
    private readonly PlayerController player;
    private readonly EntityManager entities;

    public CombatEffectsModule(EffectsManager effects, PlayerController player, EntityManager entities)
    {
        this.effects = effects;
        this.player = player;
        this.entities = entities;
        entities.OnInteraction += OnInteraction;
        entities.OnControllerDied += OnDeath;
    }

    private void OnInteraction(Controller source, Controller target, InteractionContext context)
    {
        if (!target || context.Type != InteractionType.Attack || context.Value <= 0f) return;
        if (target == player)
            effects.Play(GameplayEffect.PlayerDamage, target.transform.position);
        else if (source == player)
            effects.Play(context.IsCritical ? GameplayEffect.CriticalHit : GameplayEffect.Hit,
                target.transform.position + UnityEngine.Vector3.up * 0.8f);
    }

    private void OnDeath(Controller source, Controller target)
    {
        if (source == player && target && target != player)
            effects.Play(GameplayEffect.Kill, target.transform.position + UnityEngine.Vector3.up * 0.6f);
    }

    public void Dispose()
    {
        if (!entities) return;
        entities.OnInteraction -= OnInteraction;
        entities.OnControllerDied -= OnDeath;
    }
}
