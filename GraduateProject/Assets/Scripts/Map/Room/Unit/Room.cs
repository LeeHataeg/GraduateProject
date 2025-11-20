// Room.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;
using static Define;
using Color = UnityEngine.Color;

public class Room : MonoBehaviour
{
    public MapNode Node;
    public RectInt RoomSpace;

    public RoomType Type { get; private set; }
    public RoomState RoomState { get; private set; }
    public PortalConnection PortalConnection { get; private set; }
    public SpawnerController spawnManager { get; private set; }

    private readonly List<Portal> _portals = new();

    public RoomType RoomType;

    private void OnEnable()
    {
        if(RoomType == RoomType.Start)
        {
            GameManager.Instance.ForceEnterStartRoom();
            MinimapRenderer.NotifyPlayerEnteredRoom(this);
        }
    }

    private void SetDark()
    {
        // TMI. GetComponentsInChildren는 List가 아닌 배열을 리턴하여 아래는 틀린 문법
        //List<SpriteRenderer> sprites = GetComponentsInChildren<SpriteRenderer>();

        var tilemaps = GetComponentsInChildren<Tilemap>(true);
        var sprites = GetComponentsInChildren<SpriteRenderer>();

        foreach (var tilemap in tilemaps)
        {
            tilemap.color = new Color(0f, 0f, 0f, 1f);
        }

        foreach (var sprite in sprites)
        {
            sprite.color = new Color(0f, 0f, 0f, 1f);
        }
    }

    private void SetLight()
    {

        var tilemaps = GetComponentsInChildren<Tilemap>(true);
        var sprites = GetComponentsInChildren<SpriteRenderer>();

        foreach (var tilemap in tilemaps)
        {
            tilemap.color = new Color(1f, 1f, 1f, 1f);
        }

        foreach (var sprite in sprites)
        {
            sprite.color = new Color(1f, 1f, 1f, 1f);
        }
    }

    public void Initialize(RoomInitData init)
    {
        Node = init.Node;
        RoomSpace = init.RoomSpace;
        Type = init.RoomType;
        RoomType = init.RoomType;

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

        if(init.RoomType != RoomType.Start)
            SetDark();

        if (RoomType == RoomType.Start)
        {
            Debug.Log("[Room.cs]- 일단 분기문 들어옴. RoomType : " + RoomType);
            MinimapRenderer.NotifyPlayerEnteredRoom(this);
        }
    }

    private void OnDestroy()
    {
        if (spawnManager != null)
            spawnManager.OnAllEnemiesDefeated -= HandleAllEnemiesDefeated;
    }

    public void CachePortals()
    {
        _portals.Clear();
        GetComponentsInChildren<Portal>(true, _portals);
    }

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

        MinimapRenderer.NotifyPlayerEnteredRoom(this);

        // StartRoom/BossRoom은 포탈을 끄지 않고, 몬스터도 스폰하지 않음
        if (Type == RoomType.Normal)
        {
            SetLight();

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
