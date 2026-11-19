# Weapon stats

Each Weapon stores a serialized WeaponStats struct directly, following the JumpData
pattern with public fields and a Default initializer. Edit stats on the weapon component
or prefab in the Inspector.

Stats contain fire rate (shots/second), damage per bullet (before target defense),
magazine capacity, reload duration (seconds), range, impulse,
response time, recovery time and temporary softness.


Weapon manages ammunition, cooldown and reload progress per instance. Empty magazines
reload automatically with unlimited reserve ammunition. Call CombatHandler.Reload() or
Weapon.Reload() for a manual reload. Weapon.Reload() is virtual for subclass overrides.
Weapon.Ammunition and Weapon.IsReloading are available for UI. Misses consume ammunition.
