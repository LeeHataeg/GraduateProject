using UnityEngine;

public class BossSpawner : MonoBehaviour
{
    public BossController bossPrefab;
    public Transform spawnPoint;
    [Header("Optional")]
    public bool parentToThis = false;

    private void Awake()
    {
        Debug.Log("Pos : "+ spawnPoint.position);
    }

    public BossController Spawn()
    {
        if (!bossPrefab)
        {
            Debug.LogError("[BossSpawner] bossPrefab ¹ÌÇÒ´ç");
            return null;
        }

        var pos = spawnPoint ? spawnPoint.position : transform.position;
        var rot = spawnPoint ? spawnPoint.rotation : transform.rotation;

        var inst = Instantiate(bossPrefab, pos, rot);
        if (parentToThis) inst.transform.SetParent(transform);
        return inst;
    }
}
