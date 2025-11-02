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

    [Header("Auto Discover StartPoint")]
    [Tooltip("씬에 있는 태그를 스캔해서 자동으로 시작점을 찾아냅니다.")]
    public bool autoDiscoverStartPoint = true;

    [Tooltip("시작방(예: StartRoom)에 붙일 태그")]
    public string startRoomTag = "StartRoom";

    [Tooltip("시작방 밑에서 플레이어 스폰 지점에 붙일 태그")]
    public string playerSpawnTag = "PlayerSpawn";

    [Header("Scene Scope")]
    [Tooltip("DontDestroyOnLoad에 남아 있다면 활성 씬으로 강제 이동합니다(권장).")]
    public bool forceAttachToActiveScene = true;

    // === Rooms Cleanup (optional) ===
    [Header("Rooms Cleanup (optional)")]
    [Tooltip("생성한 방들의 루트(없으면 태그 기반으로 정리)")]
    public Transform roomsRoot;

    [Tooltip("roomsRoot가 없을 때 방으로 간주할 태그")]
    public string roomTag = "Room";

    private bool triedDiscover;
    private Coroutine _discoverCo;

    private void Awake()
    {
        // 회차/씬 전환 대비
        _startPoint = null;
        triedDiscover = false;

        // ★ 항상 활성 씬 소속으로 붙이기(DDOL에 남아 있을 가능성 차단)
        if (forceAttachToActiveScene && gameObject.scene.name == "DontDestroyOnLoad")
        {
            var active = SceneManager.GetActiveScene();
            if (active.IsValid())
            {
                SceneManager.MoveGameObjectToScene(gameObject, active);
#if UNITY_EDITOR
                Debug.Log($"[RoomManager] Force-attached to active scene '{active.name}'.");
#endif
            }
        }
    }

    private void Start()
    {
        if (autoDiscoverStartPoint && !triedDiscover)
            TryAutoDiscoverStartPoint();
    }

    private void OnDisable() => StopDiscoverCo();
    private void OnDestroy() => StopDiscoverCo();

    // ====== Public API ======

    /// <summary>시작점 지정 + 이벤트 브로드캐스트</summary>
    public void SetStartPoint(Vector2 pos)
    {
        _startPoint = pos;
#if UNITY_EDITOR
        Debug.Log($"[RoomManager] SetStartPoint({pos}) (scene={gameObject.scene.name})");
#endif
        OnSetStartPoint?.Invoke(pos);
    }

    /// <summary>회차 재시작/홈 이동 전에 호출. 시작점과(필요 시) 생성된 방들을 정리.</summary>
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
            // ★ 루트 하위만 정리(전역 삭제 금지) + 보호 대상(자식 포함) 스킵
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (IsProtectedDeep(child)) continue; // ★★★ Player가 자식에 있어도 보호

#if UNITY_EDITOR
                DestroyImmediate(child.gameObject);
#else
                Destroy(child.gameObject);
#endif
                count++;
            }
            Debug.Log($"[RoomManager] ResetRooms: cleared {count} rooms under '{root.name}'.");
        }
        else
        {
            // 활성 씬에서 태그 기반 정리 (보호 대상 스킵)
            var list = FindTaggedInActiveScene(roomTag);
            foreach (var go in list)
            {
                if (go == null) continue;
                var t = go.transform;
                if (IsProtectedDeep(t)) continue; // ★★★ Player 포함 트리 보호

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

    /// <summary>오버로드(DeathPopupUI/SendMessage 대비). 기본값 destroyRooms=true</summary>
    public void ResetRooms() => ResetRooms(true);

    /// <summary>스타트포인트 및 탐색 관련 플래그·코루틴 초기화(방 파괴는 안 함)</summary>
    public void HardReset()
    {
        _startPoint = null;
        triedDiscover = false;
        StopDiscoverCo();
#if UNITY_EDITOR
        Debug.Log("[RoomManager] HardReset: cleared start point & discovery flags.");
#endif
    }

    // ====== Discover start point ======
    public void TryAutoDiscoverStartPoint()
    {
        if (triedDiscover) return;
        triedDiscover = true;
        StopDiscoverCo();
        _discoverCo = StartCoroutine(Co_Discover());
    }

    private IEnumerator Co_Discover()
    {
        yield return null; // 씬 초기화 한 프레임 대기

        var active = SceneManager.GetActiveScene();
        if (!active.IsValid()) yield break;

        // 1) roomsRoot 하위에서 먼저 찾기
        var root = roomsRoot ?? TryFindRoomsRootInActiveScene();
        if (root != null)
        {
            var rooms = FindAllInChildrenByTag(root.transform, startRoomTag, includeInactive: true);
            if (rooms != null && rooms.Count > 0)
            {
                foreach (var room in rooms)
                {
                    var spawn = FindInChildrenByTag(room.transform, playerSpawnTag, includeInactive: true);
                    if (spawn != null) { SetStartPoint(spawn.position); yield break; }
                }
            }
        }

        // 2) 최후엔 활성 씬 전체에서 직접 찾기
        foreach (var rootGo in active.GetRootGameObjects())
        {
            var direct = FindInChildrenByTag(rootGo.transform, playerSpawnTag, includeInactive: true);
            if (direct != null) { SetStartPoint(direct.position); yield break; }
        }

#if UNITY_EDITOR
        Debug.LogWarning("[RoomManager] Auto discover failed: no start point found.");
#endif
    }

    private void StopDiscoverCo()
    {
        if (_discoverCo != null)
        {
            try { StopCoroutine(_discoverCo); } catch { }
            _discoverCo = null;
        }
    }

    // ====== Helpers ======

    // ★★★ 기본 보호 대상(자기 자신 트랜스폼만 검사)
    private static bool IsProtected(Transform t)
    {
        if (t == null) return false;
        return
            t.GetComponent<PlayerController>() != null ||
            t.GetComponent<PlayerManager>() != null ||
            t.GetComponent<GameManager>() != null ||
            t.GetComponent<UIManager>() != null ||
            t.GetComponent<RoomManager>() != null ||
            t.CompareTag("Player");
    }

    // ★★★ 확장 보호: **자식 포함**으로 Player-like/매니저류가 하나라도 있으면 보호
    private static bool IsProtectedDeep(Transform t)
    {
        if (IsProtected(t)) return true;

        // 자식 검사(비활성 포함)
        if (t.GetComponentInChildren<PlayerController>(true) != null) return true;
        if (t.GetComponentInChildren<PlayerManager>(true) != null) return true;
        if (t.GetComponentInChildren<GameManager>(true) != null) return true;
        if (t.GetComponentInChildren<UIManager>(true) != null) return true;
        if (t.GetComponentInChildren<RoomManager>(true) != null) return true;

        // 태그 "Player"가 자식 어딘가에 달려있는지 검사
        var children = t.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].CompareTag("Player")) return true;
        }
        return false;
    }

    private Transform TryFindRoomsRootInActiveScene()
    {
        var active = SceneManager.GetActiveScene();
        if (!active.IsValid()) return null;
        foreach (var go in active.GetRootGameObjects())
        {
            if (go.name.Equals("RoomsRoot", StringComparison.OrdinalIgnoreCase))
                return go.transform;
        }
        return null;
    }

    private List<GameObject> FindTaggedInActiveScene(string tag)
    {
        var active = SceneManager.GetActiveScene();
        var result = new List<GameObject>(64);
        if (!active.IsValid()) return result;

        foreach (var go in active.GetRootGameObjects())
            CollectTaggedRecursive(go.transform, tag, result, includeInactive: false);

        return result;
    }

    private void CollectTaggedRecursive(Transform t, string tag, List<GameObject> acc, bool includeInactive)
    {
        if (t == null) return;

        // ★ 확장 보호: 플레이어/매니저가 ‘자식’에 있어도 통째로 스킵
        if (IsProtectedDeep(t)) return;

        if ((includeInactive || t.gameObject.activeInHierarchy) && t.CompareTag(tag))
            acc.Add(t.gameObject);

        for (int i = 0; i < t.childCount; i++)
            CollectTaggedRecursive(t.GetChild(i), tag, acc, includeInactive);
    }

    private Transform FindInChildrenByTag(Transform root, string tag, bool includeInactive)
    {
        if (root == null) return null;
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

    private List<Transform> FindAllInChildrenByTag(Transform root, string tag, bool includeInactive)
    {
        var list = new List<Transform>();
        if (root == null) return list;

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
}
