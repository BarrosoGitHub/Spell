using UnityEngine;

/// <summary>Contact routing only; weapons need no knowledge of the ragdoll's rig or controller.</summary>
public sealed class RagdollHitbox : MonoBehaviour
{
    private HitRagdollManager owner;
    public void Initialize(HitRagdollManager manager) => owner = manager;

    public void ApplyBulletImpact(RaycastHit hit, Vector3 direction, WeaponStats data)
    {
        if (owner && owner.isActiveAndEnabled)
            owner.ApplyImpulse(hit.collider, hit.point, direction.normalized * Mathf.Max(0f, data.impulse),
                data.responseTime, data.recoveryTime, data.softness);
    }
}
