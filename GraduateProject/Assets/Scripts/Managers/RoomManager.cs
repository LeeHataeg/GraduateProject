using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-250)]
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

    [Header("Scene Scope")]
    [Tooltip("DontDestroyOnLoad에 남아 있다면 활성 씬으로 강제 이동합니다(권장).")]
    public bool forceAttachToActiveScene = true;

    private bool triedDiscover;
    private Coroutine _discoverCo;

    private void Awake()
    {
        // 회차/씬 전환 대비
        _startPoint = null;
        triedDiscover = false;

        // ★ DontDestroyOnLoad 방지: 항상 활성 씬 소속으로
        if (forceAttachToActiveScene && gameObject.scene.name == "DontDestroyOnLoad")
        {
            var active = SceneManager.GetActiveScene();
            SceneManager.MoveGameObjectToScene(gameObject, active);
#if UNITY_EDITOR
            Debug.Log($"[RoomManager] Moved to active scene '{active.name}' from DDoL.");
#endif
        }
    }

    private void OnEnable()
    {
        // 씬 타이밍에 따라 한 번 더 시도(중복돼도 무해)
        if (autoDiscoverStartPoint && !triedDiscover)
            TryAutoDiscoverStartPoint();
    }

    private void Start()
    {
        if (autoDiscoverStartPoint && !triedDiscover)
            TryAutoDiscoverStartPoint();
    }

    private void OnDisable()
    {
        StopDiscoverCo();
    }

    private void OnDestroy()
    {
        StopDiscoverCo();
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
        Debug.Log($"[RoomManager.Dump] scene={gameObject.scene.name} HasStartPoint={HasStartPoint} value={(_startPoint.HasValue ? _startPoint.Value.ToString() : "null")}");
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
        // 0) 자동탐색 코루틴/플래그 포함 전부 초기화
        HardReset();

        if (!destroyRooms)
        {
            Debug.Log("[RoomManager] ResetRooms: startPoint only.");
            return;
        }

        // 1) 방 정리
        var root = roomsRoot ?? TryFindRoomsRootInActiveScene();
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
            // 활성 씬에서만 태그 탐색
            var list = FindTaggedInActiveScene(roomTag);
            foreach (var go in list)
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

    /// <summary>
    /// 파라미터 없는 오버로드(DeathPopupUI/SendMessage 대비).
    /// 기본값(destroyRooms=true)로 방까지 정리.
    /// </summary>
    public void ResetRooms()
    {
        ResetRooms(true);
    }

    /// <summary>
    /// 스타트포인트/플래그/코루틴까지 완전 초기화(방 파괴는 안 함). 회차 전환 시 선행 호출 권장.
    /// </summary>
    public void HardReset()
    {
        _startPoint = null;
        triedDiscover = false;
        StopDiscoverCo();
#if UNITY_EDITOR
        Debug.Log("[RoomManager] HardReset: cleared start point & discovery flags.");
#endif
    }

    private void StopDiscoverCo()
    {
        if (_discoverCo != null)
        {
            StopCoroutine(_discoverCo);
            _discoverCo = null;
        }
    }

    private Transform TryFindRoomsRootInActiveScene()
    {
        string[] candidates = { "Rooms", "RoomRoot", "MapRoot", "DungeonRoot", "GeneratedRooms" };
        var active = SceneManager.GetActiveScene();
        foreach (var name in candidates)
        {
            foreach (var rootGo in active.GetRootGameObjects())
            {
                if (rootGo.name == name) return rootGo.transform;
                var t = FindInChildrenByName(rootGo.transform, name, includeInactive: true);
                if (t != null) return t;
            }
        }
        return null;
    }

    private List<GameObject> FindTaggedInActiveScene(string tag)
    {
        var list = new List<GameObject>();
        var active = SceneManager.GetActiveScene();
        foreach (var root in active.GetRootGameObjects())
        {
            CollectTaggedRecursive(root.transform, tag, list);
        }
        return list;
    }

    private void CollectTaggedRecursive(Transform t, string tag, List<GameObject> acc)
    {
        if (t.CompareTag(tag)) acc.Add(t.gameObject);
        for (int i = 0; i < t.childCount; i++)
            CollectTaggedRecursive(t.GetChild(i), tag, acc);
    }

    private Transform FindInChildrenByName(Transform root, string name, bool includeInactive)
    {
        var q = new Queue<Transform>();
        q.Enqueue(root);
        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            if ((includeInactive || cur.gameObject.activeInHierarchy) && cur.name == name)
                return cur;
            for (int i = 0; i < cur.childCount; i++)
                q.Enqueue(cur.GetChild(i));
        }
        return null;
    }

    // === 자동 탐색(선택) ===
    public void TryAutoDiscoverStartPoint()
    {
        if (triedDiscover) return;
        triedDiscover = true;
        StopDiscoverCo();
        _discoverCo = StartCoroutine(Co_AutoDiscover());
    }

    private IEnumerator Co_AutoDiscover()
    {
        // 생성/배치 순서 보정: 몇 프레임 기다리며 재시도
        for (int i = 0; i < 6 && !HasStartPoint; i++)
        {
            yield return new WaitForEndOfFrame();
            var pos = FindPlayerSpawnPositionInActiveScene();
            if (pos.HasValue)
            {
                SetStartPoint(pos.Value);
                yield break;
            }
        }
#if UNITY_EDITOR
        if (!HasStartPoint)
            Debug.LogWarning($"[RoomManager] AutoDiscover failed: Tag '{startRoomTag}' / '{playerSpawnTag}' not found in active scene.");
#endif
    }

    private Vector2? FindPlayerSpawnPositionInActiveScene()
    {
        var active = SceneManager.GetActiveScene();

        // 1) StartRoom(tag="Spawn") 우선 (활성 씬 루트들만 탐색)
        foreach (var root in active.GetRootGameObjects())
        {
            var rooms = FindAllInChildrenByTag(root.transform, startRoomTag, includeInactive: true);
            if (rooms != null && rooms.Count > 0)
            {
                foreach (var room in rooms)
                {
                    var spawn = FindInChildrenByTag(room.transform, playerSpawnTag, includeInactive: true);
                    if (spawn != null) return (Vector2)spawn.position;
                }
            }
        }

        // 2) 최후엔 활성 씬 전체에서 직접 찾기
        foreach (var root in active.GetRootGameObjects())
        {
            var direct = FindInChildrenByTag(root.transform, playerSpawnTag, includeInactive: true);
            if (direct != null) return (Vector2)direct.position;
        }
        return null;
    }

    private List<Transform> FindAllInChildrenByTag(Transform root, string tag, bool includeInactive)
    {
        var list = new List<Transform>();
        var q = new Queue<Transform>();
        q.Enqueue(root);
        while (q.Count > 0)
        {
            var t = q.Dequeue();
            if ((includeInactive || t.gameObject.activeInHierarchy) && t.CompareTag(tag))
                list.Add(t);
            for (int i = 0; i < t.childCount; i++)
                q.Enqueue(t.GetChild(i));
        }
        return list;
    }

    private Transform FindInChildrenByTag(Transform root, string tag, bool includeInactive)
    {
        var q = new Queue<Transform>();
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
