# Buffs and debuffs

Create an asset using **Assets > Create > Gameplay > Buff** and select Buff or Debuff.

- `controller.AddBuff(buff)` adds the asset and raises `OnBuffStarted`.
- `controller.RemoveBuff(buff)` removes it and raises `OnBuffFinished`.
- `controller.Buffs` exposes the active buffs for reading.
- `controller.ClearBuffs()` removes current buffs and raises finish events.

Duplicate additions are ignored. Death and disable clear buffs. Subscribe and unsubscribe at matching lifecycle boundaries.

Buffs remain active until removed. There are no timers, stacking rules, or stat modifiers.
