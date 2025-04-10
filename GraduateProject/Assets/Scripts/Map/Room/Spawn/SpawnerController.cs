using UnityEngine;


public class SpawnerController : MonoBehaviour
{
    public Transform EnemySpawnParent { get; private set; }
    public Transform ItemSpawnParent { get; private set; }
    public void Initialize(RectInt roomArea)
    {
        EnemySpawnParent = new GameObject("EnemySpawnPoints").transform;
        EnemySpawnParent.SetParent(transform);

        ItemSpawnParent = new GameObject("ItemSpawnPoints").transform;
        ItemSpawnParent.SetParent(transform);

        GenerateSpawnPoints(roomArea);
    }

    private void GenerateSpawnPoints(RectInt area)
    {
        for (int i = 0; i < 3; i++)
        {
            GameObject enemyPoint = new GameObject($"EnemySpawnPoint_{i}");
            enemyPoint.transform.SetParent(EnemySpawnParent);
            float x = Random.Range(area.xMin + 1, area.xMax - 1);
            float y = Random.Range(area.yMin + 1, area.yMax - 1);
            enemyPoint.transform.localPosition = new Vector3(x, y, 0);

            GameObject itemPoint = new GameObject($"ItemSpawnPoint_{i}");
            itemPoint.transform.SetParent(ItemSpawnParent);
            x = Random.Range(area.xMin + 1, area.xMax - 1);
            y = Random.Range(area.yMin + 1, area.yMax - 1);
            itemPoint.transform.localPosition = new Vector3(x, y, 0);
        }
    }
}
