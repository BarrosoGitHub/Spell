# Attack formula

## Enemy attack timing and hit area

`EnemyController.SetActions()` currently registers only `AttackDirectional` and the
`GettingUp` recovery action. `Attack()` selects the directional attack while the enemy
is idle or pursuing and is not traversing a navigation link. Cone and circular attack
classes remain in the project for future use.

`EnemyGroup.MaxEngagementDistance` controls the recruitment radius around the target.
The separate `EnemyGroup.AttackRange` controls how close each pursuing group member
must be to start an attack. It defaults to 3 world units and can be configured through
the group constructor or `EnemySetup.GroupSettings.attackRange`. The manager checks
each candidate's squared 3D distance, including the range boundary, and selects the
closest eligible enemies up to the simultaneous attacker limit. Out-of-range members
continue pursuing. This trigger distance is separate from the action's hit area below;
the target can still dodge during preparation.

The directional attack prepares for 0.75 seconds and executes for 0.5 seconds. On entering
preparation, the enemy stops moving and faces its target. On entering execution, it
checks its current target once and attacks if the target has an entity, is alive, and
is inside the hit area. There are no repeated damage ticks during execution.

The default range is 8 local units and the half-angle parameter is 14 degrees. Despite
the parameter name, the hit area is a rectangle, with constant half-width:

```text
halfWidth = tan(14 degrees) * 8 ≈ 1.995
hit = 0 <= localTarget.z <= 8 AND abs(localTarget.x) <= halfWidth
```

`localTarget` is the target transform position converted into the attacker's local
space. The test ignores local height and does not check line of sight, colliders, or
other nearby entities. Moving outside the rectangle before execution avoids the hit.
The preparation indicator reads range and half-angle from the directional action so
its displayed shape matches the hit area.

## Stat-based damage

Enemy hits go through `Controller.AttackController()` →
`EntityManager.TryProcessInteraction()` → `EntityManager.Attack()` → `EntityManager.CalculateDamage()`. `EntityManager.ApplyDamage()` applies defense and clamps damage to zero. `Entity.AddCurrentHealth(amount, source)` receives the final amount and source entity, clamps health, and raises health/death notifications. `CurrentHealth` is read-only. `AddCurrentHealth()` adds a signed health change: positive values heal, negative values deal damage. Healing interactions and pooled-enemy restoration use positive amounts. Attack and healing interactions pass the source entity; mission and pool restoration pass `null` because they have no source entity.

```text
baseHit = attacker.BaseDamage * (1 + attacker.BaseDamagePerc)
isCritical = chance >= 100 OR (chance > 0 AND Random.Range(0f, 100f) < chance)
rawDamage = isCritical ? baseHit * attacker.CriticalDamage : baseHit
effectiveDefense = target.Defense * (1 + target.DefensePerc)
damage = max(0, rawDamage - effectiveDefense)
newHealth = clamp(oldHealth - damage, 0, target.MaxHealth)
```

- Percentage modifiers are fractions: `0.25` means +25%. They are added to 1 before multiplication.
- `CriticalChance` uses a 0–100 scale. Chances at or below zero never crit; chances at or above 100 always crit. Other chances use a strict `<` comparison.
- `CriticalDamage` is the whole multiplier: `2` means double damage, not +200%.
- Defense subtracts a flat amount after critical damage is applied. It is not percentage damage reduction.
- Base stats and equipped item stats are added field by field, including percentage modifiers and critical multipliers, before this calculation.
- `AttackSpeed`, `AttackSpeedPerc`, and `Level` do not affect this damage formula or the directional attack's configured durations.

For example, 10 base damage with `BaseDamagePerc = 0.25` produces 12.5 damage.
A critical multiplier of 2 raises that to 25. Against 4 defense with
`DefensePerc = 0.5`, effective defense is 6: the normal hit deals 6.5 and the critical
hit deals 19.

The default stats created by `Controller.Awake()` are 5 health, 1 base damage,
0.76 defense, 0 critical chance, and a critical multiplier of 1. With no additional
stats, a hit against another default entity deals approximately 0.24 damage.

`EntityManager.ApplyDamage()` returns a nonnegative damage magnitude after defense, even if it exceeds remaining health;
health itself is clamped to zero. Death is notified only when health transitions from above zero to zero, so repeated damage or zero changes while dead do not notify again. Restoring health permits a later death notification. Health changes and death use the entity's existing
callbacks, and the interaction result reports the damage and critical flag.

## Weapon damage override

The automatic rifle supplies `WeaponStats.damagePerBullet` as `DamageOverride`.
That path skips attacker damage stats and the critical roll:

```text
damage = max(0, max(0, DamageOverride) - target.Defense * (1 + target.DefensePerc))
```

The result is always marked non-critical. Consciousness damage and physical hit
reactions are handled separately from this health damage calculation.

## Source references

- [EnemyController](../Assets/Scripts/Gameplay/Actors/Enemies/EnemyController.cs): available actions and selection.
- [AttackDirectional](../Assets/Scripts/Gameplay/Actions/Attacking/AttackDirectional.cs): timing setup and hit area.
- [Entity](../Assets/Scripts/Gameplay/Actors/Entities/Entity.cs): final damage application, health, and death notifications.
- [Stats](../Assets/Scripts/Gameplay/Actors/Entities/Stats.cs): stat fields and additive equipment aggregation.
- [EntityManager](../Assets/Scripts/Gameplay/Interaction/EntityManager.cs): stat damage calculation, critical rolls, defense mitigation, and override damage.
