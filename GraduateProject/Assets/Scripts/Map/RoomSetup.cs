using UnityEngine;

public class RoomSetup : MonoBehaviour
{
    public Transform enemySpawnPoints;
    public Transform itemSpawnPoints;

    public GameObject[] enemies;
    public GameObject[] items;

    public void Setup(RoomData room)
    {
        // 몬스터 스폰 기능은 아직 구현되지 않았으므로 주석 처리
        /*
        switch (room.RoomType)
        {
            case RoomType.Start:
                SpawnObjects(itemSpawnPoints, items, 1);
                break;
            case RoomType.Boss:
                SpawnObjects(enemySpawnPoints, new GameObject[] { enemies[0] }, 1);
                break;
            case RoomType.Normal:
                SpawnObjects(enemySpawnPoints, enemies, Random.Range(2, 5));
                break;
        }
        */
    }

    // 몬스터 및 아이템 스폰 함수도 임시로 막아둠
    /*
    void SpawnObjects(Transform parent, GameObject[] objectList, int count)
    {
        if (objectList == null || objectList.Length == 0)
        {
            Debug.LogWarning("RoomSetup: objectList가 비어 있어서 아무것도 스폰되지 않음");
            return;
        }

        List<Transform> availableSpawns = new List<Transform>(parent.GetComponentsInChildren<Transform>());
        availableSpawns.Remove(parent);

        for (int i = 0; i < count && availableSpawns.Count > 0; i++)
        {
            int index = Random.Range(0, availableSpawns.Count);
            Instantiate(objectList[Random.Range(0, objectList.Length)], availableSpawns[index].position, Quaternion.identity);
            availableSpawns.RemoveAt(index);
        }
    }
    */


}
