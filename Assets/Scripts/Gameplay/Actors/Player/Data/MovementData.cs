using System;
using UnityEngine;

[Serializable]
public struct MovementData
{
    public float maxSpeed;
    public Vector3 drag;

    public static MovementData Default => new MovementData
    {
        maxSpeed = 10f,
        drag = new Vector3(0.9f, 1f, 0.9f)
    };
}