using UnityEngine;

public class PlayerPositionController : MonoBehaviour
{
    public void MoveToStartRoom()
    {
        SetPosition(GameManager.Instance.RoomManager.StartSpawnPoint);
    }

    public void SetPosition(Vector2 spawnPosition)
    {
        Vector2 vec = spawnPosition;
        vec.y = vec.y + 1;
        GameManager.Instance.PlayerManager.Player.transform.position = vec;
    }
}
