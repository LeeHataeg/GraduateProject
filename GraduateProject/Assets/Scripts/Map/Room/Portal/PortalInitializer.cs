using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;
using static Define;

public class PortalInitializer
{
    [Header("Addressables Key for Portal Prefab")]
    [SerializeField] private string portalAddress = "Portal";
    private GameObject portalPrefab;

    // 옵션: 이름이 "Wall"인 타일맵을 우선 벽으로 간주(분리 타일맵 구조 대응)
    public string wallTilemapName = "Wall";

    // 빈 셀을 찾은 뒤 "방 안쪽"으로 더 밀어넣고 싶으면 0 이상으로 조정(타일 단위)
    public int insetFromWall = 0;

    public PortalInitializer(string address = "Portal") { portalAddress = address; }

    public void SetPortalPrefabAsync()
    {
        // 현재 프로젝트 구조 기준 Resources 사용 (경로 확인 요망)
        portalPrefab = Resources.Load<GameObject>("Prefabs/Maps/Portal/Portal");
        if (portalPrefab == null)
            throw new InvalidOperationException("Portal prefab load failed: Resources/Prefabs/Maps/Portal/Portal");
    }

    public void Init(List<Room> rooms)
    {
        if (rooms == null || rooms.Count == 0) return;

        // ★★★ 파괴/널 방 필터
        var aliveRooms = new List<Room>(rooms.Count);
        foreach (var r in rooms)
        {
            if (r != null) aliveRooms.Add(r);
        }
        if (aliveRooms.Count == 0) return;

        // 목적지 매핑
        var roomMap = new Dictionary<int, Room>(aliveRooms.Count);
        foreach (var room in aliveRooms)
        {
            if (room?.Node == null) continue;
            // Node.Id는 BSP/MST 생성 직후 재사용
            if (!roomMap.ContainsKey(room.Node.Id))
                roomMap.Add(room.Node.Id, room);
        }

        foreach (var room in aliveRooms)
        {
            if (room == null) continue;
            if (room.PortalConnection == null || room.PortalConnection.PortalInfos == null) continue;

            foreach (var kv in room.PortalConnection.PortalInfos)
            {
                var dir = kv.Key;
                var destNode = kv.Value;
                if (portalPrefab == null) { Debug.LogError("[PortalInitializer] Portal prefab is null."); continue; }

                // 안전한 parent(방 트랜스폼이 파괴되어도 여기선 살아 있음)
                var parent = room.transform;
                if (parent == null) continue; // 방이 그 사이 파괴된 경우

                var pObj = Object.Instantiate(portalPrefab, parent);
                var portal = pObj.GetComponent<Portal>();
                if (portal != null)
                    portal.Initialize(room, dir);

                // 목적지 연결
                if (destNode != null && roomMap.TryGetValue(destNode.Id, out var destRoom) && destRoom != null)
                    room.PortalConnection.ConnectRoom(dir, destRoom);

                // === 위치 계산 ===
                // 1) 방 안의 타일맵들 획득
                var tilemaps = room.GetComponentsInChildren<Tilemap>(true);
                if (tilemaps == null || tilemaps.Length == 0)
                {
                    Debug.LogError("[PortalInitializer] No Tilemap found in room.");
                    continue;
                }

                // 2) '벽' 타일맵 고르기 (이름이 Wall 우선, 없으면 첫 타일맵)
                Tilemap wallMap = null;
                foreach (var tm in tilemaps)
                {
                    if (tm && tm.gameObject.name.Equals(wallTilemapName, StringComparison.OrdinalIgnoreCase))
                    {
                        wallMap = tm;
                        break;
                    }
                }
                if (!wallMap) wallMap = tilemaps[0];
                if (!wallMap) { Debug.LogError("[PortalInitializer] Tilemap missing."); continue; }

                wallMap.CompressBounds();
                var b = wallMap.cellBounds;
                if (b.size.x <= 0 || b.size.y <= 0)
                {
                    Debug.LogWarning("[PortalInitializer] Tilemap bounds empty.");
                    continue;
                }

                // 중앙 인덱스(정수 셀 인덱스) 계산
                int cx = Mathf.Clamp((b.xMin + b.xMax - 1) / 2, b.xMin, b.xMax - 1);
                int cy = Mathf.Clamp((b.yMin + b.yMax - 1) / 2, b.yMin, b.yMax - 1);

                // 3) 벽 방향으로부터 스캔하여 "빈 셀(HasTile == false)"을 찾는다.
                Vector3Int targetCell = new Vector3Int(cx, cy, 0);

                switch (dir)
                {
                    case PortalDir.right:
                        {
                            for (int x = b.xMax - 1; x >= b.xMin; x--)
                            {
                                var c = new Vector3Int(x, cy, 0);
                                if (!wallMap.HasTile(c))
                                {
                                    c.x = Mathf.Clamp(c.x - insetFromWall, b.xMin, b.xMax - 1);
                                    targetCell = c; break;
                                }
                            }
                            break;
                        }
                    case PortalDir.left:
                        {
                            for (int x = b.xMin; x < b.xMax; x++)
                            {
                                var c = new Vector3Int(x, cy, 0);
                                if (!wallMap.HasTile(c))
                                {
                                    c.x = Mathf.Clamp(c.x + insetFromWall, b.xMin, b.xMax - 1);
                                    targetCell = c; break;
                                }
                            }
                            break;
                        }
                    case PortalDir.up:
                        {
                            for (int y = b.yMax - 1; y >= b.yMin; y--)
                            {
                                var c = new Vector3Int(cx, y, 0);
                                if (!wallMap.HasTile(c))
                                {
                                    c.y = Mathf.Clamp(c.y - insetFromWall, b.yMin, b.yMax - 1);
                                    targetCell = c; break;
                                }
                            }
                            break;
                        }
                    case PortalDir.down:
                    default:
                        {
                            for (int y = b.yMin; y < b.yMax; y++)
                            {
                                var c = new Vector3Int(cx, y, 0);
                                if (!wallMap.HasTile(c))
                                {
                                    c.y = Mathf.Clamp(c.y + insetFromWall, b.yMin, b.yMax - 1);
                                    targetCell = c; break;
                                }
                            }
                            break;
                        }
                }

                // 4) 셀 중심 월드 좌표로 배치
                var pos = wallMap.GetCellCenterWorld(targetCell);
                pObj.transform.position = pos;
            }

            // 방 내부 포탈 캐시 갱신 (room이 살아있을 때만)
            if (room != null) room.CachePortals();
        }
    }
}
