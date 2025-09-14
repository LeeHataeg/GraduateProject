using UnityEngine;

[DisallowMultipleComponent]
public class SpawnedEnemyTag : MonoBehaviour
{
    // 어느 스포너에서 생성했는지 추적용(옵션)
    public SpawnerController SourceSpawner;
}
