# Buffs and debuffs

Create an asset using **Assets > Create > Gameplay > Buff** and select Buff or Debuff. Set additive health, damage, attack speed, defense, and critical bonuses in the Inspector. Negative values reduce stats; the Buff/Debuff label does not invert values. Critical chance uses percentage points and critical damage uses an additive multiplier.

- `buff.Activate(controller)` adds the buff and starts its duration timer through `Sequence`. When the timer finishes, it calls `controller.RemoveBuff(buff)`.
- `controller.AddBuff(buff)` applies effects and raises `OnBuffStarted` without a timer. It returns whether the buff was added and remains active after notifications.
- `controller.RemoveBuff(buff)` removes effects and raises `OnBuffFinished`.
- `controller.ClearBuffs()` removes current buffs and their effects.

Duplicate additions are ignored. Death and disable clear buffs. Subscribe and unsubscribe at matching lifecycle boundaries. Calling `Activate` on an already active buff is ignored. Duration uses scaled time, including pause and slow motion; zero expires immediately. Direct `AddBuff` calls remain active until explicitly removed.

Each activation owns a sequence running on the controller and a finish subscription. Removing or clearing the buff, death, or disable cancels its sequence and unsubscribes. Action cancellation does not cancel buff sequences. Shared buff assets hold no per-controller timer state.

Stat modifiers are stored per entity, independently of base stats and equipment. They survive equipment changes and level recalculation and are removed by source asset. Different assets stack. Stat recalculation does not clamp maximum health or change current health. Changing maximum health alone does not heal, damage, or raise health/death events. Attack speed changes its stat only; existing attack code must consume that stat to change timing.

Buff effects modify stats only. `Buff.GetStats()` creates the modifier applied by the controller.
