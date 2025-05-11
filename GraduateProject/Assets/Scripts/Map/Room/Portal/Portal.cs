using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Portal : MonoBehaviour
{
    public PortalDir direction { get; private set; }
    private Room parentRoom;

    /// <summary>
    /// 맵 생성 시 RoomGenerator에서 한 번만 호출
    /// </summary>
    public void Initialize(Room room, PortalDir dir)
    {
        parentRoom = room;
        direction = dir;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var pm = other.GetComponent<PlayerMovement>();
            pm?.SetCurrentPortal(this);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var pm = other.GetComponent<PlayerMovement>();
            pm?.ClearCurrentPortal(this);
        }
    }

    /// <summary>
    /// 현재 포탈을 통해 연결된 방 반환
    /// </summary>
    public Room GetDestinationRoom()
    {
        return parentRoom.PortalConnection.ConnectedRooms[direction];
    }
}

