using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputController : TopDownCharacterController
{
    public void OnMove(InputValue value)
    {
        Vector2 direction = value.Get<Vector2>();
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
}
