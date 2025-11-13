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
    [Header("Rooms Root / Cleanup")]
    public Grid Grid;                 // RoomsRoot(Grid)
    public string roomTag = "Room";   // 폴백 태그

    private bool triedDiscover;
    private Coroutine _discoverCo;

    void Awake()
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

    void Start()
    {
        // 씬 로드 직후라도 RoomsRoot가 DDOL로 남아있지 않도록 강제
        EnsureRoomsRootIsSceneLocal();

        if (autoDiscoverStartPoint && !triedDiscover)
            TryAutoDiscoverStartPoint();
    }

    void OnDisable() => StopDiscoverCo();
    void OnDestroy() => StopDiscoverCo();

    // tilemap을 담아둘 RoomsRoot를 단독 보장
    public void EnsureRoomsRootIsSceneLocal()
    {
        if (Grid == null)
        {
            // 씬 안에 RoomsRoot가 있으면 붙잡아옴
            var found = TryFindRoomsRootInActiveScene();
            if (found == null)
            {
                var go = new GameObject("RoomsRoot", typeof(Grid));
                Grid = go.GetComponent<Grid>();
                SceneManager.MoveGameObjectToScene(go, SceneManager.GetActiveScene());
#if UNITY_EDITOR
                Debug.Log("[RoomManager] RoomsRoot(Grid) created in active scene.");
#endif
            }
            else
            {
                Grid = found.GetComponent<Grid>();
#if UNITY_EDITOR
                Debug.Log("[RoomManager] RoomsRoot(Grid) bound from active scene.");
#endif
            }
            return;
        }

        // Grid가 이미 있는데 DDOL에 있으면 강제로 옮김
        if (Grid.gameObject.scene.name == "DontDestroyOnLoad")
        {
            var active = SceneManager.GetActiveScene();
            SceneManager.MoveGameObjectToScene(Grid.gameObject, active);
#if UNITY_EDITOR
            Debug.Log("[RoomManager] Moved RoomsRoot(Grid) from DDOL to active scene.");
#endif
        }

        if (!string.Equals(Grid.gameObject.name, "RoomsRoot", StringComparison.OrdinalIgnoreCase))
            Grid.gameObject.name = "RoomsRoot";
    }

    public void SetStartPoint(Vector2 pos)
    {
        _startPoint = pos;
#if UNITY_EDITOR
        Debug.Log($"[RoomManager] SetStartPoint({pos}) (scene={gameObject.scene.name})");
#endif
        OnSetStartPoint?.Invoke(pos);
    }


    // 즉시 파괴/정리(위험) 대신, 항상 코루틴 버전을 사용하도록 유도
    public void ResetRooms(bool destroyRooms = true)
    {
        // 호환용: 내부적으로 코루틴 호출
        StartCoroutine(Co_ResetRooms(destroyRooms));
    }

    public IEnumerator Co_ResetRooms(bool destroyRooms)
    {
        HardReset();
        EnsureRoomsRootIsSceneLocal();

        if (!destroyRooms)
        {
#if UNITY_EDITOR
            Debug.Log("[RoomManager] Co_ResetRooms: startPoint only.");
#endif
            yield break;
        }

        // 한 프레임 비켜서(물리/애니메이션 콜백 회피)
        yield return null;

        int count = 0;
        Transform root = Grid ? Grid.transform : TryFindRoomsRootInActiveScene();

        if (root != null)
        {
            // RoomsRoot 하위 전부 안전하게 Destroy
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (IsProtectedDeep(child)) continue;
                Destroy(child.gameObject);
                count++;
            }
#if UNITY_EDITOR
            Debug.Log($"[RoomManager] Co_ResetRooms: cleared {count} rooms under '{root.name}'.");
#endif
        }
        else
        {
            // 폴백: 태그 기반으로 제거
            var list = FindTaggedInActiveScene(roomTag);
            foreach (var go in list)
            {
                if (!go) continue;
                var t = go.transform;
                if (IsProtectedDeep(t)) continue;
                Destroy(go);
                count++;
            }
#if UNITY_EDITOR
            Debug.Log($"[RoomManager] Co_ResetRooms: cleared {count} tagged('{roomTag}') rooms (no roomsRoot).");
#endif
        }
    }

    public void HardReset()
    {
        _startPoint = null;
        triedDiscover = false;
        StopDiscoverCo();
#if UNITY_EDITOR
        Debug.Log("[RoomManager] HardReset: cleared start point & discovery flags.");
#endif
    }

    public void TeleportToSpawnPoint(Transform target)
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

        EnsureRoomsRootIsSceneLocal();
        var root = Grid ? Grid.transform : TryFindRoomsRootInActiveScene();

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

        foreach (var go in active.GetRootGameObjects())
        {
            var direct = FindInChildrenByTag(go.transform, playerSpawnTag, includeInactive: true);
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

    // ───────── helpers / guards ─────────
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
            if (go.name.Equals("RoomsRoot", StringComparison.OrdinalIgnoreCase))
                return go.transform;

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
