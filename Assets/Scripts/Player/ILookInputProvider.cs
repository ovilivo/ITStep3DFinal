using UnityEngine;

/// <summary>
/// Abstraction over look input. CameraFollowHandler depends on this
/// instead of PlayerController directly, keeping the camera decoupled from
/// any specific player type.
/// </summary>
public interface ILookInputProvider
{
    Vector2 LookInput { get; }
}
