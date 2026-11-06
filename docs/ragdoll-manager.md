# Hit ragdoll manager

`HitRagdollManager` owns torque tracking, pose blending and contact impulses.
`RagdollManager` and `EnemyPhysicalHitReaction` remain disabled on `Enemy_normal`.

## Modules

- `HitRagdollManager.Setup.cs`: one-time humanoid rig, colliders and joints.
- `HitRagdollManager.cs`: runtime torque, pose blending and impacts.
- `HitRagdollManager.Recovery.cs`: optional Controller integration for unconsciousness and get-up.
- `RagdollHitbox.cs`: forwards confirmed bullet contacts.

Assign a Humanoid Animator. Physics Parent and Root Collider are optional. The
project prefab uses the gameplay capsule as Physics Parent so hitboxes retain their
damage owner. The manager does not depend on either legacy ragdoll component.

## Strength controls

Effective strength is `Overall × Region × Recovery`, before temporary hit response.
Upper Body includes spine, head and arms; Lower Body includes pelvis and legs.

- 0: free ragdoll.
- Between 0 and 1: damped torque following animation, with linear visible pose blending.
- 1: animation only, with kinematic physics bodies.

Overall, Upper Body and Lower Body are manual sliders. Consciousness does not
continuously change them. The manager handles torque, impacts and knockout recovery.

## Knockout and get-up

With the optional Controller assigned, zero consciousness enters Unconscious and
sets Recovery to zero. All limbs ragdoll regardless of slider values. The existing
controller waits until consciousness reaches 100 to start GettingUp.

Get-up preparation chooses the face-up/down animation, holds its first frame and
raises Recovery from zero to one over Recovery Blend Duration, capped to preparation
time. The same torque and pose-blending system returns the body to its configured
slider strengths. Execution releases the animation. Another knockout interrupts it.

The root follows the fallen pelvis horizontally. Root alignment preserves physics
world poses and momentum, avoiding dragging the rig when the gameplay root moves.

Without a Controller the manager remains manual. `SetRagdollActive(bool)` and
`BeginRecovery(duration)` expose recovery for other integrations. The three public
`Set...Strength(float)` methods can be wired to UI sliders.

## Impacts

`ApplyImpulse(collider, point, impulse, responseTime, recoveryTime, softness)` accepts
an explicit world-space impulse and optional response parameters per struck limb.
Bullet tuning comes from the weapon status asset, forwarded by `RagdollHitbox`.
Joint limits and tracking coefficients are internal defaults; the Inspector exposes
setup, the three strength sliders and recovery settings.
Accepted hits align the connected physics bodies to the last displayed limbs before
applying force at contact, retaining momentum. A smoothed temporary reduction in the
struck limb's strength allows impact response. Fully animated limbs ignore impulses.

Enemy movement is handled separately by EnemyController.
