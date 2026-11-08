using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class IceParticleDamage : MonoBehaviour
{
    public event Action<ParticleCollisionEvent> OnImpact;

    private readonly List<ParticleCollisionEvent> collisions = new List<ParticleCollisionEvent>(8);
    private ParticleSystem particles;
    private Func<GameObject, bool> confirmContact;

    public void Initialize(ParticleSystem particleSystem, Func<GameObject, bool> contactCallback)
    {
        particles = particleSystem;
        confirmContact = contactCallback;
    }

    private void OnParticleCollision(GameObject other)
    {
        if (particles == null)
            return;

        int count = particles.GetCollisionEvents(other, collisions);
        for (int i = 0; i < count; i++)
        {
            if (confirmContact == null || !confirmContact(other)) continue;
            OnImpact?.Invoke(collisions[i]);
        }
        collisions.Clear();
    }

    private void OnDisable()
    {
        particles = null;
        confirmContact = null;
        collisions.Clear();
    }
}
