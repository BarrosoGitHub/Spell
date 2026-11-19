using UnityEngine;

/// <summary>Maps a physics hitbox to the visible bone used by ice impact decoration.</summary>
public sealed class RagdollHitbox : MonoBehaviour
{
    public Transform VisibleBone { get; private set; }

    public void Initialize(Transform visibleBone = null)
    {
        VisibleBone = visibleBone;
    }
}
