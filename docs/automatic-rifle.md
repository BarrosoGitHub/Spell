# Automatic rifle example

Add CombatHandler alongside PlayerController, assign Weapon Socket and Starting Weapon, and set Weapon Offset (a local position applied on equip). PlayerController requests shots while AttackPressed is held. The weapon owns range and cooldown. TryExecuteAttack takes a world-space aim point, not a direction vector.

`PlayerAutoAttack` is enabled on the Player 1 prefab. Every 0.15 scaled seconds it
selects the nearest living enemy collider within weapon range that is on camera and
visible from both the camera and weapon transform. It aims at that collider's center and
requests shots without mouse input, rechecking visibility each frame. Walls and other
solid objects block detection and firing. It uses the normal weapon cooldown,
ammunition, reload, and damage flow. Manual input continues to aim at the mouse.
Auto-fire runs while the living player is moving or hanging and stops during other
actions, pause, loss of application focus, or a non-Playing game state. It calls the
existing CombatHandler and Controller APIs without modifying the player or weapon
scripts; the weapon's own raycast still checks obstruction from its actual muzzle. Disable the
PlayerAutoAttack component in the Inspector to return to manual firing. View Camera,
Target Mask, and Target Search Interval Seconds configure detection. Query buffers
grow to 512 entries and use a complete allocating query on rare overflow.

The player projects the mouse cursor through the camera and aims at the nearest non-trigger surface in Aim Mask, ignoring the player and equipped weapon. When no surface is under the cursor, aiming falls back to a horizontal plane at the muzzle height. Shots travel from the actual muzzle toward that point; the weapon's collision check still stops them at intervening walls and enforces weapon range. Missing mouse/camera or a cursor outside the camera viewport does not fire.

## Shooting arm pose

Weapon.OnShotFired publishes the accepted shot's world-space origin and endpoint, including misses. CombatHandler relays this as OnShoot and detaches from the old weapon when unequipping. PlayerRiggingController subscribes while enabled and aims its existing RArmTarget along that direction, within the right arm's reach. The existing Player 1 prefab already assigns the target, right-arm constraint, and arms rig.

Shooting Aim in the rigging Inspector controls the pose hold time (0.2 seconds), release blend (0.12 seconds), and hand rotation offset. Increase hold time for weapons with longer intervals between shots. The rig restores its previous weights and target pose after firing. Climbing actions release shooting IK before their entry handlers run; shooting while hanging leaves the climbing pose intact. Character facing is unchanged.

Assign the muzzle ParticleSystem on AutomaticRifle. It supplies the shot origin and plays its child emitters on each accepted shot. The rifle keeps the muzzle reusable rather than allowing the imported effect to destroy itself.

## Bullet and wall effects

Accepted shots automatically emit a moving gold particle rendered as a stretched billboard, including misses. Bullet travel, impact sparks, and dust all use ParticleSystem; no LineRenderer is used. Bullet Effects in the rifle Inspector controls speed (120 units/s by default), length (1.2), width (0.025), and color. An optional Shot Effect Material overrides the supplied URP material in Resources/WeaponShotEffect.mat; no additional prefab setup is required.

The visual tracer ends at the same nearest raycast hit used by damage. Hits on scenery (colliders with no parent Controller) produce an outward spark burst and fading dust when the tracer reaches the surface. Misses and character hits do not create wall effects. Impact particles are offset along the surface normal and simulate in world space.

Damage remains hitscan and is applied immediately through Controller.AttackController and InteractionManager. Tracer travel delays only the visuals. This is not a physics projectile, and moving geometry is not re-tested during visual travel.

BulletShotEffects maintains a scene-local pool capped at 128 simultaneous effects. Tracers can finish after a weapon is unequipped. Effects and their pool are destroyed with the scene. Existing raycast targeting allocates RaycastAll results once per accepted shot. The simple weapon cooldown permits at most one shot per frame.

## Play-mode verification

- Fire into a wall: each muzzle burst should launch a tracer, followed by sparks/dust at the surface.
- Fire into open space: the tracer should reach maximum range without an impact burst.
- Fire at an enemy: the tracer should stop on the enemy and existing damage events should fire; no wall dust.
- Hold fire: bursts should repeat at the weapon rate and the pooled object count should level off.
- Move away from the world origin and aim around the player: shots should follow the cursor independently of character facing.
- Aim at an enemy or wall: the bullet should travel toward the surface under the cursor; a wall between muzzle and target must still block it.
- Aim over empty space: the shot should use the muzzle-height plane. Check this with a tilted camera too.
- Fire around the player and above/below the muzzle: the right arm should point along the tracer, including shots that miss.
- Stop firing: the arm should blend back after the hold time. Disable/re-enable the rig or swap weapons: no duplicate shot callbacks should occur.
- Start climbing while firing: the grab should place the hands on the ledge, and shots while hanging should not move them.

C# compilation has been checked. Shader appearance and play-mode behavior still need verification in Unity.
