using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class SpawnerController : MonoBehaviour
{
    private Tilemap _tilemap;
    private List<Vector3Int> _spawnCells;

    public void Initialize(Tilemap tilemap, List<Vector3Int> spawnCells)
    {
        _tilemap = tilemap;
        _spawnCells = spawnCells;
    }

    public void SpawnEnemies()
    {
        var prefab = Resources.Load<GameObject>("Prefabs/Enemies/Enemy_Skul_Normal");
        if (prefab == null) { Debug.LogError("Enemy 프리팹 로딩 실패"); return; }

        foreach (var cell in _spawnCells)
        {
            // 셀 → 월드 변환
            Vector3 worldPos = _tilemap.CellToWorld(cell) + _tilemap.cellSize * 0.5f;
            Instantiate(prefab, worldPos, Quaternion.identity, transform);
        }
    }
}