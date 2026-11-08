# Ice weapon

Player 1 equips `Assets/Prefab/IceWeapon.prefab`. Test 1 inherits this setup from Player 1; the Enemy Hit Reactions player override also uses it. Imported ParticlePack assets remain unchanged.

## Modules and Actions

Controller.Shoot is the shared manual/automatic firing entry point. TryExecuteAttack returns a bool: true for an accepted shot, including a miss, and false for cooldown, invalid aim/source or missing weapon. Controller raises `event Action<Weapon, Vector3> OnShoot` only when that bool is true. Hit confirmation travels separately through OnAttackConfirmed for both ice projectiles and rifle hits. PlayerRiggingController listens to OnShoot to point the arm and blend its weight in, then uses Delay to blend it out.


- `IceWeapon` validates casts and enforces cooldown. Its `OnCast` event supplies origin, velocity, stats and a cached contact callback.
- `IceProjectileDisplay` owns and emits the particle effect. It passes the callback to `IceParticleDamage`, which reports contacts to its caller and raises `OnImpact` for accepted contacts. Neither component references or attacks a Controller.
- `IceWeapon` resolves the contacted Controller, rejects self hits and invalid combatants, applies weapon consciousness damage and raises the inherited `OnAttackConfirmed` event with target and interaction context.
- `CombatHandler` forwards confirmation to its owning `Controller`, which calls `AttackController` and routes the interaction to EntityManager. The rifle confirms hits synchronously through the same event path.
- `IceImpactDisplay` listens to `OnImpact` solely for embedded shard decoration.

```mermaid
sequenceDiagram
    Controller->>CombatHandler: TryExecuteAttack
    CombatHandler->>IceWeapon: TryExecuteAttack
    IceWeapon->>IceProjectileDisplay: OnCast + contact callback
    IceProjectileDisplay->>IceParticleDamage: Initialize callback and emit
    IceParticleDamage-->>IceWeapon: Contact callback
    IceWeapon-->>CombatHandler: OnAttackConfirmed
    CombatHandler-->>Controller: OnAttackConfirmed
    Controller->>EntityManager: AttackController / TryProcessInteraction
```

Callbacks return contact to the weapon without a reference from presentation to the controller. Confirmation subscriptions are paired across enable/disable and weapon replacement. Controller resolves CombatHandler in Awake before subscribing in OnEnable. Cancellation clears the weapon's source; disabling the display clears its particles and contact callback. Impact decoration is optional.

## Prefab setup and tuning

The IceWeapon prefab contains three components: IceWeapon, IceProjectileDisplay and IceImpactDisplay. Its serialized references are already connected. The adapter creates the IceParticleDamage component on the instantiated ParticleSystem root, where Unity sends collision messages.

Configure fire rate, damage and range in IceWeapon’s Stats, and Projectile Speed on IceWeapon. The existing `damagePerBullet` field supplies spell damage; magazine/reload fields are unused. The starter weapon casts twice per second at the main-scene effect’s 80 units/second, with 50-unit range and 0 damage before defense. Casting is unlimited.

Assign the ParticlePack IceLance asset, Effect Scale, Shard Rotation Offset and Hit Mask on IceProjectileDisplay. The effect uses the pack’s ice meshes, materials, mist and collision/death sub-emitters. The casting sphere is hidden; lance meshes and particle children remain visible. Effect Scale is 0.25, reducing flying shards, embedded shards and particle children to one quarter of their original size while preserving variation (0.5–1 instead of 2–4). World-aligned particle rotations face each accepted cast’s velocity, correcting the mesh’s negative-X tip with a 90-degree Y offset. The original randomized roll is preserved. Existing particles retain their own facing when the player aims elsewhere.

The lifetime is range divided by speed. Native particle collision stops ice against scenery and enemies. On collision the flying particle expires and raises OnImpact, which places a persistent embedded shard. The pack’s mist/shatter sub-emitters remain in use. The flying particle is killed on contact, preventing resting contacts. If a collision reports zero velocity, the surface normal supplies the embedded shard’s direction. Ignore Raycast (layer 2) is excluded because it contains the player capsule; update Hit Mask if player layers change. The core emitter is capped at 64 particles and remains simulated offscreen. Damage is delivered on collision, with no freezing, slowing or damage over time.

IceImpactDisplay is enabled on the prefab. Embedded crystals reuse the lance mesh and shared materials and use RagdollHitbox’s visible-bone mapping to transfer the contact point and orientation from physics-limb space to the rendered bone, then become children of that bone. This follows the final animation/ragdoll blend without changing physics bodies or blend logic. Other surfaces use the contacted collider transform. Collision detection still uses the existing physics pose. Random sampled sizes are multiplied by 0.225 (0.1125–0.225 at the current effect scale), with up to roughly 12 degrees of random tilt. Embed Depth is 0.2 units. The display owns a bounded ring of 128 shard objects, creates slots on demand and reuses them once full. Shards are removed when their target is disabled or destroyed, preventing attachments from surviving a pooled enemy respawn; empty slots are recreated on reuse. Disabling or destroying the weapon display clears its attached shards. This replaces the world-space particle ring with parentable mesh objects; it retains more GameObjects to support attachment.


## Validation

The confirmation flow and lifecycle subscriptions were inspected statically. Supplemental C# compilation is checked separately from Unity Play Mode. Final shard orientation, embedding, repeated enable/disable and weapon replacement require Play Mode verification.

The visual reference is `ParticlePack/Scenes/Main Scene with Legacy Particles.unity`. The player adaptation keeps the lance, randomized size/roll, speed and particle children, omits the casting sphere, and uses Action-driven persistent embedded impacts instead of relying on the demo’s short remaining particle lifetime. Player aiming, cast rate and detection range remain gameplay settings. The original imported prefab and scene are unchanged.
