# SpiderOrange leg rig

The SpiderOrange prefab has a RigBuilder on the Animator root and an active LegRig.

Hierarchy:
- LegRig/Targets: Leg01Target through Leg04Target.
- LegRig/Constraints: Leg01IK through Leg04IK (Chain IK).
- LegRig/Constraints/FootFollowers: Leg01Foot through Leg04Foot (Multi-Parent).

| Leg | Chain root | Chain tip | Separate foot bone |
| --- | --- | --- | --- |
| 01 | Bone | Bone.002_end | Foot |
| 02 | Bone.003 | Bone.005_end | Foot.001 |
| 03 | Bone.006 | Bone.008_end | Foot.002 |
| 04 | Bone.009 | Bone.011_end | Foot.003 |

Each Chain IK constraint includes all three leg segments. Targets start at the imported endpoints. The separate weighted foot bones follow their solved endpoints, with their original position and rotation offsets retained. Targets live outside the animated skeleton; move a target to pose a leg.

The rig supplies manual leg posing. It does not add procedural stepping or ground detection.

The setup editor script has been removed. The saved rig uses Unity Animation Rigging components and requires no custom setup script.

Validation in Unity 6000.6.2f1: the rig graph built, all four endpoint targets preserved the rest pose, each leg followed a moved target with less than 0.00006 units of endpoint error, and the separate foot bones remained attached to their solved endpoints.
