using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerActionController : MonoBehaviour
{
    public event Action<bool> onJumpEvent;

    bool isPressed;
    public void CallJumpEvent(bool isPressed)
    {
        onJumpEvent?.Invoke(isPressed);
    }

    public void OnJump(InputValue value)
    {
        isPressed = value.Get<float>() > 0f;
        CallJumpEvent(isPressed);
        Debug.Log("OnJump!");

    }
}
