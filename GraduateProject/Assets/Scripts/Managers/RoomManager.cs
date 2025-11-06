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
    public bool autoDiscoverStartPoint = true;
    public string startRoomTag = "StartRoom";
    public string playerSpawnTag = "PlayerSpawn";

    [Header("Scene Scope")]
    public bool forceAttachToActiveScene = true;

    // === Rooms Cleanup (optional) ===
    [Header("Rooms Cleanup (optional)")]
    public Grid Grid;
    public string roomTag = "Room";

    // 진행 상태
    private bool triedDiscover;
    private Coroutine _discoverCo;

    // 방 정리 동작 상태
    private bool _isClearing;
    public event Action OnRoomsCleared; // 외부가 기다릴 수 있게 이벤트 제공

    private void Awake()
    {
        _startPoint = null;
        triedDiscover = false;

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

    public void SetStartPoint(Vector2 pos)
    {
        _startPoint = pos;
#if UNITY_EDITOR
        Debug.Log($"[RoomManager] SetStartPoint({pos}) (scene={gameObject.scene.name})");
#endif
        OnSetStartPoint?.Invoke(pos);
    }

    /// <summary>
    /// 방 정리를 예약한다(안전 시점에 실행). 트리거/물리 콜백 중에도 호출 OK.
    /// </summary>
    public void ResetRooms(bool destroyRooms = true)
    {
        // 코루틴으로 미뤄서 안전 시점에 수행
        StartCoroutine(Co_ResetRooms(destroyRooms));
    }

    /// <summary>
    /// 방 정리를 수행하고 완료까지 기다리는 코루틴.
    /// </summary>
    public IEnumerator Co_ResetRooms(bool destroyRooms = true)
    {
        // 중복 진입 방지
        if (_isClearing)
            yield break;

        _isClearing = true;

        // 물리/렌더/애니메이션 콜백을 모두 벗어나기 위해 한 프레임 말미까지 대기
        yield return new WaitForFixedUpdate();
        yield return new WaitForEndOfFrame();

        HardReset(); // 시작점/플래그 초기화

        if (!destroyRooms)
        {
#if UNITY_EDITOR
            Debug.Log("[RoomManager] ResetRooms: startPoint only.");
#endif
            _isClearing = false;
            OnRoomsCleared?.Invoke();
            yield break;
        }

        // null-safe roomsRoot
        var root = (Grid != null ? Grid.transform : null) ?? TryFindRoomsRootInActiveScene();
        int count = 0;

        if (root != null)
        {
            // 파괴 대상 수집 후 Destroy (즉시 파괴 금지)
            var toDestroy = new List<GameObject>(root.childCount);
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (IsProtectedDeep(child)) continue;
                toDestroy.Add(child.gameObject);
            }

            foreach (var go in toDestroy)
            {
                if (go) Destroy(go); // ★ DestroyImmediate 사용 금지
            }

            count = toDestroy.Count;
#if UNITY_EDITOR
            Debug.Log($"[RoomManager] ResetRooms: cleared {count} rooms under '{root.name}'.");
#endif
        }
        else
        {
            // roomsRoot가 없을 때 태그 기반 폴백
            var list = FindTaggedInActiveScene(roomTag);
            foreach (var go in list)
            {
                if (go == null) continue;
                var t = go.transform;
                if (IsProtectedDeep(t)) continue;
                Destroy(go); // ★ Destroy
                count++;
            }
#if UNITY_EDITOR
            Debug.Log($"[RoomManager] ResetRooms: cleared {count} tagged('{roomTag}') rooms (no roomsRoot).");
#endif
        }

        // 파괴가 실제로 반영되도록 한 프레임 정도 더 대기
        yield return null;

        _isClearing = false;
        OnRoomsCleared?.Invoke();
    }

    public void ResetRooms() => ResetRooms(true);

    public void HardReset()
    {
        _startPoint = null;
        triedDiscover = false;
        StopDiscoverCo();
#if UNITY_EDITOR
        Debug.Log("[RoomManager] HardReset: cleared start point & discovery flags.");
#endif
    }

    public void TeleportToStart(Transform target)
    {
        if (!HasStartPoint || !target) return;
        target.position = GetStartPoint();
    }

    public void TryAutoDiscoverStartPoint()
    {
        if (triedDiscover) return;
        triedDiscover = true;
        StopDiscoverCo();
        _discoverCo = StartCoroutine(Co_Discover());
    }

    private IEnumerator Co_Discover()
    {
        yield return null;

        var active = SceneManager.GetActiveScene();
        if (!active.IsValid()) yield break;

        var root = (Grid != null ? Grid.transform : null) ?? TryFindRoomsRootInActiveScene();
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

    private static bool IsProtectedDeep(Transform t)
    {
        if (IsProtected(t)) return true;

        if (t.GetComponentInChildren<PlayerController>(true) != null) return true;
        if (t.GetComponentInChildren<PlayerManager>(true) != null) return true;
        if (t.GetComponentInChildren<GameManager>(true) != null) return true;
        if (t.GetComponentInChildren<UIManager>(true) != null) return true;
        if (t.GetComponentInChildren<RoomManager>(true) != null) return true;

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
            if (go.name.Equals("RoomsRoot", System.StringComparison.OrdinalIgnoreCase))
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
