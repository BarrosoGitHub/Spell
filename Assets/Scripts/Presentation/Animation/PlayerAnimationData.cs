using System;
using UnityEngine;

[Serializable]
public sealed class PlayerAnimationData
{
    [Header("Body follow and tilt")]
    [Min(0f)] public float positionFollowSpeed = 12f;
    [Min(0f)] public float rotationFollowSpeed = 7f;
    public float accelerationTiltStrength = 0.12f;
    [Min(0f)] public float maxTiltAngle = 12f;
    [Min(0f)] public float tiltResponseSpeed = 4f;
    public float heightOffset = 1f;

    [Header("Hanging swing")]
    [Min(0.1f)] public float hangingPendulumLength = 0.9f;
    [Range(0f, 2f)] public float hangingForwardDamping = 0.35f;
    [Range(0f, 2f)] public float hangingSideDamping = 0.35f;
    [Range(0f, 45f)] public float hangingForwardAngleLimit = 22f;
    [Range(0f, 45f)] public float hangingBackwardAngleLimit = 22f;
    [Range(0f, 45f)] public float hangingSideAngleLimit = 22f;
    [Min(0f)] public float hangingGrabImpulse = 0.35f;
    [Range(0f, 20f)] public float grabTiltAngle = 8f;
    [Tooltip("Angular acceleration from movement input, in degrees per second squared.")]
    [Min(0f)] public float hangingMovementStrength = 35f;

    [Header("Hanging spring")]
    public bool springEnabled = true;
    [Tooltip("Restoring acceleration per metre of displacement. Higher values give a faster bounce.")]
    [Range(1f, 400f)] public float springStiffness = 80f;
    [Tooltip("Damping ratio: 0 keeps bouncing, 1 is critically damped.")]
    [Range(0f, 2f)] public float springDamping = 0.3f;
    [Tooltip("Maximum extra distance away from the hold, in metres.")]
    [Min(0f)] public float springStretchLimit = 0.2f;
    [Tooltip("Maximum displacement toward the hold, in metres.")]
    [Min(0f)] public float springCompressionLimit = 0.1f;
    [Tooltip("Fraction of grab velocity along the hold-to-body direction transferred to the spring.")]
    [Min(0f)] public float springGrabImpulse = 0.2f;
    [Tooltip("Response to acceleration of the player root.")]
    [Min(0f)] public float springAccelerationStrength = 0.3f;
    [Tooltip("Stretching acceleration from movement input while hanging, in metres per second squared.")]
    [Min(0f)] public float springMovementStrength = 0.5f;
}

// Visual displacement from the existing hanging pose; positive is away from the hold.
public struct HangingSpringState
{
    public float offset;
    public float velocity;

    public void Step(PlayerAnimationData data, float drive, float deltaTime)
    {
        if (!data.springEnabled)
        {
            this = default;
            return;
        }
        float stiffness = Mathf.Clamp(data.springStiffness, 1f, 400f);
        float damping = 2f * Mathf.Clamp(data.springDamping, 0f, 2f) * Mathf.Sqrt(stiffness);
        // Implicit damping remains stable at high damping values.
        velocity = (velocity + (drive - stiffness * offset) * deltaTime) / (1f + damping * deltaTime);
        offset += velocity * deltaTime;
        float minimum = -Mathf.Max(0f, data.springCompressionLimit);
        float maximum = Mathf.Max(0f, data.springStretchLimit);
        if (offset < minimum)
        {
            offset = minimum;
            if (velocity < 0f) velocity = 0f;
        }
        if (offset > maximum)
        {
            offset = maximum;
            if (velocity > 0f) velocity = 0f;
        }
    }
}
