using UnityEngine;

public class RoomState : MonoBehaviour
{
    public bool IsCleared { get; private set; }

    public void RoomCleared()
    {
        IsCleared = true;
    }
}