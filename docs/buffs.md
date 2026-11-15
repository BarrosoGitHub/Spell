# Buffs and debuffs

Create an asset using **Assets > Create > Gameplay > Buff** and select Buff or Debuff. Set additive health, damage, attack speed, movement speed, defense, and critical bonuses in the Inspector. Negative values reduce stats; the Buff/Debuff label does not invert values. Critical chance uses percentage points and critical damage uses an additive multiplier.

- `controller.AddBuff(buff)` applies the modifier, raises `OnBuffStarted`, and schedules expiry. `EntityManager.AddBuff(controller, buff)` and `buff.Activate(controller)` delegate to it. All three return whether the activation was accepted, including immediate expiry.
- `controller.RemoveBuff(buff)` removes effects and raises `OnBuffFinished`.
- `controller.ClearBuffs()` removes current buffs and their effects.

Duplicate additions are ignored. Death and disable clear buffs. Subscribe and unsubscribe at matching lifecycle boundaries. Calling `Activate` on an already active buff is ignored. Duration uses scaled time, including pause and slow motion; zero expires immediately.

Each activation owns a sequence running on the controller. Removing or clearing the buff, death, or disable cancels its sequence. Action cancellation does not cancel buff sequences. Shared buff assets hold no per-controller timer state.

Stat modifiers are stored per entity, independently of base stats and equipment. They survive equipment changes and level recalculation and are removed by source asset. Different assets stack. Stat recalculation does not clamp maximum health or change current health. Changing maximum health alone does not heal, damage, or raise health/death events. The next health change clamps current health between zero and the effective maximum, with a minimum cap of zero. Attack speed changes its stat only; existing attack code must consume that stat to change timing.

Buff effects modify stats only. `Buff.GetStats()` creates the modifier applied by the controller.

`Stats.MovementSpeed` is additive and drives player locomotion and enemy NavMesh walking. Player base speed is initialized from the existing `movementData.maxSpeed`; enemy base walking speed is 2.5 for both idle following and pursuit. This replaces any separate idle NavMeshAgent speed. Negative totals are treated as zero by movement consumers without changing stat values. Dodge and climbing retain their own movement settings.
