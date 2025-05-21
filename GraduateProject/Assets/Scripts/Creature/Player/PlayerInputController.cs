using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputController : CharacterController
{

    #region MOVE_VARIABLES
    // Move
    Vector2 moveDir;
    //Look
    Vector2 lookDir;
    Vector2 mousePos;
    //Jump
    float isPressed;
    float isHit;
    #endregion

    public void OnMove(InputValue value)
    {
        moveDir = value.Get<Vector2>();
        moveDir = moveDir.normalized;
        CallMoveEvent(moveDir);
    }

    public void OnLook(InputValue value)
    {
        mousePos = value.Get<Vector2>();
        mousePos = Camera.main.ScreenToWorldPoint(mousePos);
        lookDir = mousePos - (Vector2)transform.position;
        CallLookEvent(lookDir);
    }

    public void OnHit(InputValue value)
    {
        isHit = value.Get<float>();
        CallHitEvent(isHit > 0f);
    }

    public void OnJump(InputValue value)
    {
        isPressed = value.Get<float>();
        CallJumpEvent(isPressed > 0f);

    }

    public void OnInteract(InputValue value)
    {
        isPressed = value.Get<float>();
        CallInteractEvent(isPressed > 0f);
    }

    public void OnDash(InputValue value)
    {
        isPressed = value.Get<float>();
        CallDashEvent(isPressed > 0f);
    }

    public void OnTeleport(InputValue value)
    {
        isPressed = value.Get<float>();
        CallTeleportEvent(isPressed > 0f);
    }

    public void OnCrunch(InputValue value)
    {
        isPressed = value.Get<float>();
        CallCrunchEvent(isPressed > 0f);
    }
}
