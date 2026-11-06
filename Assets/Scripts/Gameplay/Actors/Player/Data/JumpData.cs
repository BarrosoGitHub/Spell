using UnityEngine;
using System;

[Serializable]
public struct JumpData
{
    public float jumpPower;
    public float jumpBufferTime;
    public float coyoteTime;
    public float fallGravityMultiplier;
    public Vector3 gravityDirection;
    public float gravityScale;
    public float lastJumpPressed;
    public float lastJumpReleased;
    public float lastGrounded;
    public bool canJump;
    public bool canCutoff;

    public static JumpData Default => new JumpData
    {
        jumpPower = 5f,
        jumpBufferTime = 0.12f,
        coyoteTime = 0.1f,
        fallGravityMultiplier = 2f,
        gravityDirection = Vector3.up,
        gravityScale = -9.81f,
        lastJumpPressed = -5f,
        lastJumpReleased = -5f,
        canJump = true,
        canCutoff = false
    };
}