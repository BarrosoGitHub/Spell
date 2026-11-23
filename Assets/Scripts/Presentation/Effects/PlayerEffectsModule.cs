using System;
using UnityEngine;

public sealed class PlayerEffectsModule : IDisposable
{
    private readonly EffectsManager effects;
    private readonly PlayerController player;
    private bool wasAirborne;
    private float airborneSince;

    public PlayerEffectsModule(EffectsManager effects, PlayerController player)
    {
        this.effects = effects;
        this.player = player;
        player.OnShoot += OnShoot;
        player.OnExecutingActionInstance += OnExecuting;
        player.OnGroundedChanged += OnGrounded;
    }

    private void OnShoot(Weapon weapon, Vector3 aimPoint)
    {
        effects.Play(GameplayEffect.Shot, weapon ? weapon.transform.position : player.transform.position);
    }

    private Vector3 GroundPosition => player.groundHit.collider
        ? player.groundHit.point + Vector3.up * 0.08f : player.transform.position;

    private void OnExecuting(float duration, ActionInstance action)
    {
        if (action is Dodge) effects.Play(GameplayEffect.Dodge, GroundPosition);
    }

    private void OnGrounded(bool grounded)
    {
        if (!grounded)
        {
            if (!wasAirborne) airborneSince = Time.time;
            wasAirborne = true;
            return;
        }
        if (wasAirborne && Time.time - airborneSince > 0.2f)
            effects.Play(GameplayEffect.Landing, GroundPosition);
        wasAirborne = false;
    }

    public void Dispose()
    {
        if (!player) return;
        player.OnShoot -= OnShoot;
        player.OnExecutingActionInstance -= OnExecuting;
        player.OnGroundedChanged -= OnGrounded;
    }
}
