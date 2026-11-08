using System.Collections.Generic;
using UnityEngine;

// One-time rig construction. Runtime pose sampling, torque and impacts live in the main file.
public sealed partial class HitRagdollManager
{
    private sealed class Limb
    {
        public Transform bone, end;
        public Rigidbody body;
        public CapsuleCollider collider;
        public Joint joint;
        public HumanBodyBones boneId;
        public Limb parent;
        public bool lowerBody;
        public Vector3 parentSegmentDirection, segmentDirection, connectedAnchor;
        public Vector3 position, localPosition, targetVelocity, targetAngularVelocity, physicsLocalPosition;
        public Quaternion rotation, localRotation, physicsLocalRotation;
        public float impactRemaining, impactWeight, impactVelocity;
        public float impactResponseTime, impactRecoveryTime, impactSoftness;
        public Pose visiblePose;
    }

    private bool InitializeRig()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!animator || !animator.isHuman || !animator.GetBoneTransform(HumanBodyBones.Hips))
        {
            Debug.LogError("HitRagdollManager requires a Humanoid Animator with a hips bone.", this);
            return false;
        }
        CreatePhysicsRig();
        return true;
    }

    private void CreatePhysicsRig()
    {
        physicsRoot = new GameObject("Torque ragdoll rig").transform;
        // Hit colliders belong to the gameplay controller even when the visual is its sibling.
        physicsRoot.SetParent(physicsParent ? physicsParent : transform, false);
        AddLimb(HumanBodyBones.Hips, HumanBodyBones.Spine, 0.15f);
        AddLimb(HumanBodyBones.Spine, HumanBodyBones.Neck, 0.17f);
        AddLimb(HumanBodyBones.Head, HumanBodyBones.LastBone, 0.11f);
        AddLimb(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, 0.065f);
        AddLimb(HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, 0.05f);
        AddLimb(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, 0.065f);
        AddLimb(HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, 0.05f);
        AddLimb(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, 0.09f);
        AddLimb(HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, 0.065f);
        AddLimb(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, 0.09f);
        AddLimb(HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, 0.065f);
        var limbsByBone = new Dictionary<Transform, Limb>(limbs.Count);
        foreach (var limb in limbs) limbsByBone.Add(limb.bone, limb);
        foreach (var limb in limbs)
        {
            for (var p = limb.bone.parent; p && limb.parent == null; p = p.parent)
                limbsByBone.TryGetValue(p, out limb.parent);
            if (limb.parent == null) continue;

            if (IsHinge(limb.boneId))
            {
                CreateHinge(limb);
                continue;
            }
            var joint = limb.body.gameObject.AddComponent<ConfigurableJoint>();
            limb.joint = joint;
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = Vector3.zero;
            limb.connectedAnchor = limb.parent.body.transform.InverseTransformPoint(limb.body.position);
            joint.connectedAnchor = limb.connectedAnchor;
            joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Locked;
            joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Limited;
            joint.lowAngularXLimit = new SoftJointLimit { limit = -65f };
            joint.highAngularXLimit = new SoftJointLimit { limit = 65f };
            joint.angularYLimit = new SoftJointLimit { limit = 80f };
            joint.angularZLimit = new SoftJointLimit { limit = 80f };
            joint.projectionMode = JointProjectionMode.PositionAndRotation;
            joint.projectionDistance = 0.08f;
            ConnectLimb(limb);
        }
        for (int i = 0; i < limbs.Count; i++)
        {
            if (rootCollider) Physics.IgnoreCollision(rootCollider, limbs[i].collider);
            for (int j = i + 1; j < limbs.Count; j++) Physics.IgnoreCollision(limbs[i].collider, limbs[j].collider);
        }
    }

    private static bool IsKnee(HumanBodyBones boneId) =>
        boneId == HumanBodyBones.LeftLowerLeg || boneId == HumanBodyBones.RightLowerLeg;

    private static bool IsHinge(HumanBodyBones boneId) => IsKnee(boneId) ||
        boneId == HumanBodyBones.LeftLowerArm || boneId == HumanBodyBones.RightLowerArm;

    private void CreateHinge(Limb limb)
    {
        Vector3 upperDirection = (limb.bone.position - limb.parent.bone.position).normalized;
        Vector3 lowerDirection = limb.end
            ? (limb.end.position - limb.bone.position).normalized : upperDirection;
        // Knees fold backward, elbows forward in the model's reference pose.
        Vector3 bendDirection = animator.transform.forward * (IsKnee(limb.boneId) ? -1f : 1f);
        Vector3 expectedAxis = Vector3.Cross(upperDirection, bendDirection);
        if (expectedAxis.sqrMagnitude < 0.001f) expectedAxis = animator.transform.right;
        Vector3 axis = Vector3.Cross(upperDirection, lowerDirection);
        if (axis.sqrMagnitude < 0.001f) axis = expectedAxis;
        if (Vector3.Dot(axis, expectedAxis) < 0f) axis = -axis;

        var hinge = limb.body.gameObject.AddComponent<HingeJoint>();
        limb.joint = hinge;
        hinge.autoConfigureConnectedAnchor = false;
        hinge.anchor = Vector3.zero;
        hinge.axis = limb.body.transform.InverseTransformDirection(axis.normalized);
        limb.connectedAnchor = limb.parent.body.transform.InverseTransformPoint(limb.body.position);
        hinge.connectedAnchor = limb.connectedAnchor;
        limb.parentSegmentDirection = limb.parent.body.transform.InverseTransformDirection(upperDirection);
        limb.segmentDirection = limb.body.transform.InverseTransformDirection(lowerDirection);
        hinge.useLimits = true;
        ConnectLimb(limb);
    }

    private void ConnectLimb(Limb limb)
    {
        limb.joint.connectedBody = limb.parent.body;
        limb.joint.connectedAnchor = limb.connectedAnchor;
        if (limb.joint is HingeJoint hinge)
        {
            // Rebase limits on the current pose whenever physics reconnects the joint.
            // A bent animated limb must not become the new straight/zero-flexion pose.
            Vector3 axis = limb.body.rotation * hinge.axis;
            float flexion = Vector3.SignedAngle(limb.parent.body.rotation * limb.parentSegmentDirection,
                limb.body.rotation * limb.segmentDirection, axis);
            float straightAngle = hinge.angle - flexion;
            float maxFlexion = IsKnee(limb.boneId) ? kneeFlexionLimit : elbowFlexionLimit;
            hinge.limits = new JointLimits
            {
                min = straightAngle,
                max = straightAngle + maxFlexion,
                bounciness = 0f,
                contactDistance = 2f
            };
        }
    }

    private void AddLimb(HumanBodyBones boneId, HumanBodyBones endId, float radius)
    {
        var bone = animator.GetBoneTransform(boneId);
        if (!bone) return;
        var end = endId == HumanBodyBones.LastBone ? null : animator.GetBoneTransform(endId);
        var obj = new GameObject(boneId + " hitbox");
        obj.layer = 0;
        obj.transform.SetParent(physicsRoot, false);
        obj.transform.SetPositionAndRotation(bone.position, bone.rotation);
        var body = obj.AddComponent<Rigidbody>();
        body.mass = 1f;
        body.isKinematic = true;
        body.useGravity = false;
        body.maxAngularVelocity = 30f;
        body.solverIterations = 12;
        body.solverVelocityIterations = 4;
        obj.AddComponent<RagdollHitbox>().Initialize(this, bone);
        var collider = obj.AddComponent<CapsuleCollider>();
        var offset = end ? Quaternion.Inverse(bone.rotation) * (end.position - bone.position) : Vector3.up * 0.16f;
        collider.direction = Mathf.Abs(offset.x) > Mathf.Abs(offset.y) ? 0 : 1;
        if (Mathf.Abs(offset.z) > Mathf.Max(Mathf.Abs(offset.x), Mathf.Abs(offset.y))) collider.direction = 2;
        collider.center = offset * 0.5f;
        collider.radius = radius;
        collider.height = Mathf.Max(radius * 2f, offset.magnitude + radius);
        limbs.Add(new Limb
        {
            boneId = boneId,
            lowerBody = boneId == HumanBodyBones.Hips || boneId == HumanBodyBones.LeftUpperLeg ||
                boneId == HumanBodyBones.LeftLowerLeg || boneId == HumanBodyBones.RightUpperLeg || boneId == HumanBodyBones.RightLowerLeg,
            end = end,
            bone = bone,
            body = body,
            collider = collider,
            position = bone.position,
            rotation = bone.rotation,
            localRotation = bone.localRotation,
            localPosition = bone.localPosition
        });
    }
}
