using UnityEngine;
using System;

[Serializable]
public struct ClimbingData
{
    public float jumpDistance;

    public static ClimbingData Default => new ClimbingData
    {
        jumpDistance = 1.5f
    };
}