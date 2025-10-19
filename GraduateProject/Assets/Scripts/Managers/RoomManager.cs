using System;
using System.Collections;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    // === SpawnPoint ===
    public event Action<Vector2> OnSetStartPoint;

    private Vector2? _startPoint;
    public bool HasStartPoint => _startPoint.HasValue;
    public Vector2 GetStartPoint() => _startPoint ?? Vector2.zero;

    [Header("Auto Discover (optional)")]
    [Tooltip("씬 진입 직후 StartRoom(Tag=Spawn) 하위의 PlayerSpawn(Tag=PlayerSpawn)을 자동 탐색해 1회 SetStartPoint 호출")]
    public bool autoDiscoverStartPoint = true;

    [Tooltip("StartRoom에 부여된 태그명")]
    public string startRoomTag = "Spawn";

    [Tooltip("PlayerSpawn에 부여된 태그명")]
    public string playerSpawnTag = "PlayerSpawn";

    private bool triedDiscover;

    private void Awake()
    {
        // 회차 전환 대비
        _startPoint = null;
        triedDiscover = false;
    }

    private void Start()
    {
        if (autoDiscoverStartPoint) TryAutoDiscoverStartPoint();
    }

    private void OnEnable()
    {
        // 씬 타이밍에 따라 한 번 더 시도(중복돼도 무해)
        if (autoDiscoverStartPoint && !triedDiscover) TryAutoDiscoverStartPoint();
    }

    public void SetStartPoint(Vector2 pos)
    {
        _startPoint = pos;
        Debug.Log($"[RoomManager] SetStartPoint({pos}) (scene={gameObject.scene.name})");
        OnSetStartPoint?.Invoke(pos);
    }

    [ContextMenu("DEBUG: Dump State")]
    public void DebugDump()
    {
        Debug.Log($"[RoomManager.Dump] HasStartPoint={HasStartPoint} value={(_startPoint.HasValue ? _startPoint.Value.ToString() : "null")}");
    }

    // === 회차/씬 재진입 준비: 시작점 & 방 정리 ===
    [Header("Rooms Cleanup (optional)")]
    [Tooltip("생성한 방들의 루트(없으면 태그 기반으로 정리)")]
    public Transform roomsRoot;
    [Tooltip("roomsRoot가 없을 때 방으로 간주할 태그")]
    public string roomTag = "Room";

    /// <summary>
    /// 회차 재시작/홈 이동 전에 호출. 시작점과(필요 시) 생성된 방들을 정리.
    /// </summary>
    public void ResetRooms(bool destroyRooms = true)
    {
        // 1) ★★ 핵심: 시작점 초기화
        _startPoint = null;
        triedDiscover = false;

        if (!destroyRooms)
        {
            Debug.Log("[RoomManager] ResetRooms: startPoint only.");
            return;
        }

        // 2) 방 정리
        var root = roomsRoot ?? TryFindRoomsRoot();
        int count = 0;

        if (root != null)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
#if UNITY_EDITOR
                DestroyImmediate(root.GetChild(i).gameObject);
#else
                Destroy(root.GetChild(i).gameObject);
#endif
                count++;
            }
            Debug.Log($"[RoomManager] ResetRooms: cleared {count} rooms under '{root.name}'.");
        }
        else
        {
            var tagged = GameObject.FindGameObjectsWithTag(roomTag);
            foreach (var go in tagged)
            {
#if UNITY_EDITOR
                DestroyImmediate(go);
#else
                Destroy(go);
#endif
                count++;
            }
            Debug.Log($"[RoomManager] ResetRooms: cleared {count} tagged('{roomTag}') rooms (no roomsRoot).");
        }
    }

    private Transform TryFindRoomsRoot()
    {
        string[] candidates = { "Rooms", "RoomRoot", "MapRoot", "DungeonRoot", "GeneratedRooms" };
        foreach (var name in candidates)
        {
            var go = GameObject.Find(name);
            if (go != null) return go.transform;
        }
        return null;
    }

    // === 자동 탐색(선택) ===
    public void TryAutoDiscoverStartPoint()
    {
        if (triedDiscover) return;
        triedDiscover = true;
        StartCoroutine(Co_AutoDiscover());
    }

    private IEnumerator Co_AutoDiscover()
    {
        // 생성 순서 보정: 몇 프레임 기다리며 재시도
        for (int i = 0; i < 6 && !HasStartPoint; i++)
        {
            yield return new WaitForEndOfFrame();
            var pos = FindPlayerSpawnPosition();
            if (pos.HasValue)
            {
                SetStartPoint(pos.Value);
                yield break;
            }
        }
#if UNITY_EDITOR
        if (!HasStartPoint)
            Debug.LogWarning($"[RoomManager] AutoDiscover failed: Tag '{startRoomTag}' / '{playerSpawnTag}' not found.");
#endif
    }

    private Vector2? FindPlayerSpawnPosition()
    {
        // 1) StartRoom(tag="Spawn") 우선
        var startRooms = GameObject.FindGameObjectsWithTag(startRoomTag);
        if (startRooms != null && startRooms.Length > 0)
        {
            foreach (var room in startRooms)
            {
                var spawn = FindInChildrenByTag(room.transform, playerSpawnTag, includeInactive: true);
                if (spawn != null) return (Vector2)spawn.position;
            }
        }
        // 2) 최후엔 씬 전체에서 직접 찾기
        var direct = GameObject.FindGameObjectWithTag(playerSpawnTag);
        if (direct != null) return (Vector2)direct.transform.position;
        return null;
    }

    private Transform FindInChildrenByTag(Transform root, string tag, bool includeInactive)
    {
        var q = new System.Collections.Generic.Queue<Transform>();
        q.Enqueue(root);
        while (q.Count > 0)
        {
            var t = q.Dequeue();
            if ((includeInactive || t.gameObject.activeInHierarchy) && t.CompareTag(tag))
                return t;
            for (int i = 0; i < t.childCount; i++)
                q.Enqueue(t.GetChild(i));
        }
        return null;
    }
}
