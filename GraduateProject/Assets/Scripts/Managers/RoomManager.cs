using System;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public Vector2 StartSpawnPoint;

    const string objectName = "StartRoom(Clone)";
    const string tagNameStartRoom = "Spawn";

    public event Action<Vector2> OnSetStartPoint;

    private void Awake()
    {
        StartSpawnPoint = new Vector2();
    }

    public void SetStartPoint(Vector2 pos)
    {
        StartSpawnPoint = pos;
        OnSetStartPoint?.Invoke(pos);
    }
}
