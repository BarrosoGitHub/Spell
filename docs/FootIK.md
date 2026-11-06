# Ground foot planting

`RiggingController` now drives grounded feet through the existing left/right Two Bone IK constraints. Its ground logic lives in `PlayerRiggingController.GroundFeet.cs`; no extra component or scene wiring is needed when the Animator, RigBuilder, leg constraints, and PlayerController are already assigned.

The Animator and RigBuilder keep Unity's normal automatic evaluation. Ground-foot targets update in LateUpdate and are applied by the next rig evaluation. Legacy Animator foot IK weights are cleared to prevent a second solver from competing. Do not add a second IK solver to the same feet.

A foot close to the ground is planted at a stored contact point. That contact follows the surface transform, including moving platforms. Animated lift, lost ground, excessive reach, or a large separation releases it. Swing feet blend back to the animation. Leaving the movement state releases ground ownership before climbing updates its targets and weights.

The **Run-to-idle catch** prevents the familiar final slide when a running clip blends into idle. When horizontal speed crosses below **Stop Speed** after exceeding **Minimum Run Speed**, reachable feet near the floor are captured over **Stop Catch Window** and blend to their locked targets using **Stop Blend In Time**. This is separate from ordinary stance detection, so it still works without animation contact curves.

Settings are under **Ground foot planting** on RiggingController:

- **Sole Height** is the ankle-to-ground distance in world units; start at 0.08 and tune it to the character.
- **Plant Height / Release Height** control how close the animated sole must be to plant and how far it can lift before releasing. Release Height should exceed Plant Height.
- **Maximum Plant Distance / Maximum Leg Extension** prevent a foot from remaining pinned behind an unreachable leg.
- **Blend In / Out Time** smooth contact transitions; **Replant Delay** prevents immediate re-locking during takeoff.
- **RayMask** selects walkable colliders, with Ground Mask as a fallback when empty. Triggers and player colliders are ignored.
- Existing **Weight Position Left/Right** and **Weight Rotation Left/Right** control each foot. Rotation weights of zero keep animated foot orientation; raising them enables planted slope alignment.
- Optional **Left / Right Plant Curve** names refer to Animator float parameters driven by animation curves: 1 during stance, 0 during swing. These provide more precise timing than automatic height detection, especially for shuffling animations. Missing named parameters fall back to automatic detection with a warning.

Leg constraints must have Maintain Target Position Offset and Maintain Target Rotation Offset disabled. The existing scene already uses these settings. The automatic path is intended for the scene's normal per-frame Animator evaluation; separately scheduled animation evaluation needs coordinated pose sampling.

No stride length, movement speed, or animation playback rate is changed. Contact locking reduces stance sliding; tuning contact timing and leg reach still matters for the chosen character and clips.
