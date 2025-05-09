using System;
using UnityEngine;

public class CharacController : MonoBehaviour
{
    #region EVENT_ACTION
    public event Action<Vector2> OnMoveEvent;
    public event Action<Vector2> OnLookEvent;
    public event Action<bool> OnJumpEvent;
    public event Action<bool> OnInteractEvent;
    public event Action<bool> OnDashEvent;
    public event Action<bool> OnTeleportEvent;
    #endregion

    public void CallMoveEvent(Vector2 direction)
    {
        OnMoveEvent?.Invoke(direction);
    }

    public void CallLookEvent(Vector2 direction)
    {
        OnLookEvent?.Invoke(direction);
    }

    public void CallJumpEvent(bool isPressed)
    {
        OnJumpEvent?.Invoke(isPressed);
    }

    public void CallInteractEvent(bool isInteract)
    {
        OnInteractEvent?.Invoke(isInteract);
    }

    public void CallDashEvent(bool isDash)
    {
        OnDashEvent?.Invoke(isDash);
    }

    public void CallTeleportEvent(bool isTeleport)
    {
        OnTeleportEvent?.Invoke(isTeleport);
    }
}
