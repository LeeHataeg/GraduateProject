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
    bool isGround = true;
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

    public void OnJump(InputValue value)
    {
        isPressed = value.Get<float>();
        CallJumpEvent(isPressed);
    }

    // TODO - tempcode (Need to Refactoring)
    private void OnCollisionEnter2D(Collision2D coll)
    {
        if (coll.gameObject.CompareTag("Ground"))
        {
            isGround = true;
            CallGroundEvent(isGround);
        }
    }
}
