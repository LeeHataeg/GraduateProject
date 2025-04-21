using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public Vector2 StartSpawnPoint;

    const string objectName = "StartRoom(Clone)";
    const string tagNameStartRoom = "Spawn";

    private void Awake()
    {
        StartSpawnPoint = new Vector2();
    }

    public void SetStartPoint(Vector2 pos)
    {
        StartSpawnPoint = pos;
        //Debug.Log("할당 완료");
    }
}
