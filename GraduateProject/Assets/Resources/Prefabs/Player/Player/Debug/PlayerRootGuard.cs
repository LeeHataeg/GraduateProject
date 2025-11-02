using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class PlayerRootGuard : MonoBehaviour
{
    [Header("Debug")]
    public bool verbose = true;

    private Transform roomsRoot;
    private Transform anchor;
    private Scene cachedScene;
    private float nextRefreshAt; // refs 재탐색 쿨다운

    private void OnEnable()
    {
        cachedScene = gameObject.scene.IsValid() ? gameObject.scene : SceneManager.GetActiveScene();
        RefreshRefs(force: true);
        // 활성화 즉시 1회 보정
        DetachIfUnderRoomsRoot();
    }

    private void Update()
    {
        // 씬이 바뀌었거나, 아직 roomsRoot/anchor를 못 찾았으면 주기적으로 재탐색
        if (!cachedScene.IsValid() || cachedScene != (gameObject.scene.IsValid() ? gameObject.scene : SceneManager.GetActiveScene()))
        {
            cachedScene = gameObject.scene.IsValid() ? gameObject.scene : SceneManager.GetActiveScene();
            roomsRoot = null; anchor = null;
            nextRefreshAt = 0f;
        }

        if (Time.unscaledTime >= nextRefreshAt || roomsRoot == null || anchor == null)
            RefreshRefs(force: false);

        // 매 프레임 가드
        DetachIfUnderRoomsRoot();
    }

    private void OnTransformParentChanged()
    {
        // 부모가 바뀌는 즉시 가드
        DetachIfUnderRoomsRoot();
    }

    private void RefreshRefs(bool force)
    {
        nextRefreshAt = Time.unscaledTime + (force ? 0.25f : 0.5f);

        // 1) RoomManager에서 직접 참조
        var rm = FindFirstObjectByType<RoomManager>(FindObjectsInactive.Include);
        if (rm != null && rm.roomsRoot != null) roomsRoot = rm.roomsRoot;

        // 2) 못 찾았으면 이름으로
        if (roomsRoot == null)
        {
            foreach (var go in cachedScene.GetRootGameObjects())
            {
                if (go.name.Equals("RoomsRoot", System.StringComparison.OrdinalIgnoreCase))
                {
                    roomsRoot = go.transform;
                    break;
                }
            }
        }

        // 3) 앵커 보장
        anchor = EnsureAnchor(cachedScene);

        if (verbose)
        {
            var parentName = transform.parent ? transform.parent.name : "<null>";
            Debug.Log($"[PlayerRootGuard] RefreshRefs: roomsRoot={(roomsRoot ? roomsRoot.name : "<null>")} " +
                      $"anchor={(anchor ? anchor.name : "<null>")} scene={cachedScene.name} parent={parentName}");
        }
    }

    private static Transform EnsureAnchor(Scene scene)
    {
        const string AnchorName = "__PLAYER_ANCHOR__";
        foreach (var go in scene.GetRootGameObjects())
            if (go.name == AnchorName) return go.transform;

        var anchorGO = new GameObject(AnchorName);
        SceneManager.MoveGameObjectToScene(anchorGO, scene);
        return anchorGO.transform;
    }

    private void DetachIfUnderRoomsRoot()
    {
        if (roomsRoot == null) return;

        if (IsUnder(transform, roomsRoot))
        {
            // 씬 루트로 뽑고 → 앵커 밑으로
            transform.SetParent(null, true);
            SceneManager.MoveGameObjectToScene(gameObject, cachedScene);
            if (anchor == null) anchor = EnsureAnchor(cachedScene);
            transform.SetParent(anchor, true);

            if (verbose)
            {
                Debug.Log($"[PlayerRootGuard] Player was under '{roomsRoot.name}' → moved under '{anchor.name}'. " +
                          $"pathNow={GetPath(transform)}");
            }
        }
    }

    private static bool IsUnder(Transform child, Transform root)
    {
        var p = child.parent;
        while (p != null)
        {
            if (p == root) return true;
            p = p.parent;
        }
        return false;
    }

    private static string GetPath(Transform t)
    {
        if (!t) return "<null>";
        var s = t.name;
        var p = t.parent;
        while (p)
        {
            s = p.name + "/" + s;
            p = p.parent;
        }
        return "/" + s;
    }
}
