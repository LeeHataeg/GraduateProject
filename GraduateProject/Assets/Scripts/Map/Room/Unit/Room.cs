// Room.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    public MapNode Node;
    public RectInt RoomSpace;

    public RoomType Type { get; private set; }
    public RoomState RoomState { get; private set; }
    public PortalConnection PortalConnection { get; private set; }
    public SpawnerController spawnManager { get; private set; }

    // ▼ 추가: 이 방 안의 포탈 캐시
    private readonly List<Portal> _portals = new();

    public void Initialize(RoomInitData init)
    {
        Node = init.Node;
        RoomSpace = init.RoomSpace;
        Type = init.RoomType;

        RoomState = gameObject.AddComponent<RoomState>();
        PortalConnection = gameObject.AddComponent<PortalConnection>();

        spawnManager = gameObject.GetComponent<SpawnerController>();
        if (spawnManager != null)
        {
            // ▼ 전부 처치 시: 클리어 + 포탈 열기
            // (이벤트 이름은 OnAllEnemiesDefeated 입니다!)
            spawnManager.OnAllEnemiesDefeated += HandleAllEnemiesDefeated;
        }

        PortalConnection.Initialize(init.Node.Portals);
    }

    private void OnDestroy()
    {
        if (spawnManager != null)
            spawnManager.OnAllEnemiesDefeated -= HandleAllEnemiesDefeated;
    }

    // ▼ 포탈 목록 캐시(PortalInitializer가 포탈 생성한 뒤 1회 호출해줄 것)
    public void CachePortals()
    {
        _portals.Clear();
        GetComponentsInChildren<Portal>(true, _portals);
    }

    // ▼ Normal방의 포탈만 on/off (Start/Boss는 항상 on)
    public void SetPortalsActive(bool active)
    {
        if (Type == RoomType.Start || Type == RoomType.Boss) return;

        foreach (var p in _portals)
        {
            if (p && p.TryGetComponent<Collider2D>(out var col))
                col.enabled = active;

            // 선택: 시각 피드백(없으면 생략해도 무방)
            var sr = p.GetComponentInChildren<SpriteRenderer>();
            if (sr) sr.color = active ? Color.white : new Color(1, 1, 1, 0.35f);
        }
    }

    private void HandleAllEnemiesDefeated()
    {
        RoomState.RoomCleared();
        SetPortalsActive(true); // 모든 몬스터 처치 → 포탈 열림
    }

    public Vector2 GetSpawnPosition()
    {
        Vector2 lowerLeft = (Vector2)transform.position;
        Vector2 middle = lowerLeft + new Vector2(RoomSpace.width * 0.5f, RoomSpace.height * 0.5f);
        return middle;
    }

    public void OnPlayerEnter()
    {
        // StartRoom/BossRoom은 포탈을 끄지 않고, 몬스터도 스폰하지 않음
        if (Type == RoomType.Normal)
        {
            if (!RoomState.IsCleared)
            {
                SetPortalsActive(false);     // 입장 시 포탈 OFF
                spawnManager?.SpawnEnemies(); // 처음만 스폰됨(Spawner가 내부적으로 1회 보장)
            }
            else
            {
                SetPortalsActive(true);      // 재방문(클리어 방) → 포탈 유지
            }
        }
    }
}
