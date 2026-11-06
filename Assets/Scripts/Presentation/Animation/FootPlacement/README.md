# Standalone foot placement

The player prefab uses `FootPlacementIK` on its Humanoid Animator and `BreaklineFootPlacement` to feed the grounded movement state. The Footwerk package, generated rig and integration have been removed; this system is standalone.

`FootPlacementIK` runs in the base layer's Animator IK pass. It reads the animated foot positions, casts two downward rays against the player's ground mask, aligns each foot to the surface, and locks near-ground stance feet in the hit object's local coordinates. Locks therefore follow moving/rotating platforms. Swing height, upward foot movement, lost ground and excessive horizontal stretch release a lock. A short cooldown prevents immediately re-locking after a release. Ground correction fades in/out; the animated pelvis can lower slightly to reduce leg overextension. No physics or root transform is written.

`BreaklineFootPlacement` enables the solver only while the player is enabled, grounded and in `movingState`. Missing input, disabling the adapter, jumping, hanging and actions release both feet. Existing climbing leg constraints and arm/aim rigs remain intact. Leave base-layer **IK Pass** enabled in the Animator Controller; don't enable a second foot solver on the character.

## Tuning on the Animator object

- **Sole Offset**: small shoe clearance above the surface, added to Unity's Humanoid foot-bottom height. Default 1.5 cm.
- **Plant Height / Release Height**: animated foot clearance used to detect stance and swing. Defaults 7 / 16 cm. Increase release height only if the animation lifts a planted foot slightly during stance.
- **Maximum Lock Distance**: releases a foot when its lock is too far from the animated ankle. Default 30 cm; reduce it if legs stretch during sharp turns.
- **Release Upward Speed**: how quickly an animated foot must rise to start releasing. Default 0.3 m/s.
- **Maximum Pelvis Drop**: maximum animated pelvis adjustment. Default 18 cm. Set to zero to disable.
- **Lock Planted Feet**: turn off to retain only ground alignment.
- **Use Foot Placement** on the adapter: switches the complete system off.

This uses animation height/movement to estimate contacts; it does not require added animation curves. It is not a gait generator or stride-warping system. Large mismatches between clip speed and capsule speed still need appropriate locomotion clips/playback timing. Test walking/running, stopping, stairs, platform movement and sharp turns with your animations. Ground queries ignore triggers and use the player's ground mask; keep the character's own colliders off those layers.

For another character, add both components to its Humanoid Animator, assign its player to the adapter and enable base-layer IK Pass. Other controllers can feed `SetGroundedMovement(active, groundMask)` every Update using their own adapter.
