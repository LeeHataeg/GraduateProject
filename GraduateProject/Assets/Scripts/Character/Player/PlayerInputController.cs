using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputController : TopDownCharacterController
{
    bool isGround = true;

    //temp-Val

    public void OnMove(InputValue value)
    {
        Vector2 direction;
        if (isGround)
        {
            direction = value.Get<Vector2>();
            direction.y = 0;
        }
        else
        {
            direction = value.Get<Vector2>();
        }
        direction = direction.normalized;
        CallMoveEvent(direction);
        Debug.Log("CalledMove");
    }
    // 마우스가 움직일 때마다 호출된다
    public void OnLook(InputValue value)
    {
        // 설정해 두었던 mouse의 위치를 받아온다
        // 이때 이 위치를 Screen 위치므로 주의해야 한다
        Vector2 mousePosition = value.Get<Vector2>();
        mousePosition = Camera.main.ScreenToWorldPoint(mousePosition);
        Vector2 direction = mousePosition - (Vector2)transform.position;
        CallLookEvent(direction);
    }

    private void OnCollisionEnter2D(Collision2D coll)
    {
        if (coll.gameObject.CompareTag("Ground"))
        {
            isGround = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if(collision.gameObject.CompareTag("Ground"))
            isGround = false;
    }
}
