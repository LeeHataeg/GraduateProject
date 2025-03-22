using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterController : MonoBehaviour
{
    #region EVENT_ACTION
    public event Action<Vector2> OnMoveEvent;
    public event Action<Vector2> OnLookEvent;
    public event Action<float> OnJumpEvent;
    public event Action<bool> OnGroundEvent;
    #endregion

    public void CallMoveEvent(Vector2 direction)
    {
        OnMoveEvent?.Invoke(direction);
    }
    public void CallLookEvent(Vector2 direction)
    {
        OnLookEvent?.Invoke(direction);
    }
    public void CallJumpEvent(float isPressed)
    {
        OnJumpEvent?.Invoke(isPressed);
    }
    public void CallGroundEvent(bool isGround)
    {
        OnGroundEvent?.Invoke(isGround);
    }
}
