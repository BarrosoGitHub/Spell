using System;

[Serializable]
public struct HoverData
{
    public float rideHeight;
    public float springStrength;
    public float springDamper;
    public float groundCheckDistance;
    public float groundCheckRadius;

    public static HoverData Default => new HoverData
    {
        rideHeight = 1f,
        springStrength = 300f,
        springDamper = 10f,
        groundCheckDistance = 1.35f,
        groundCheckRadius = 0.1f
    };
}