using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    [Header("Player Prefab & Spawn")]
    public GameObject playerPrefab;                                   // 에디터에서 지정
    public string playerResourcesPath = "Prefabs/Player/Player/Player";
    [Tooltip("인게임 씬 이름")]
    public string gameplaySceneName = "InGameScene";

    [Header("Runtime Refs")]
    [Tooltip("최상위 깡통(움직이지 않음)")]
    public GameObject Player;                                          // Player(깡통)
    [Tooltip("실제 물리 이동 루트(여기만 이동)")]
    public GameObject UnitRoot;                                        // UnitRoot

    // 내부 상태
    private bool _forceFreshSpawn;                                     // 다음 씬에서 반드시 새 스폰
    private bool _spawnedThisScene;                                    // 이번 씬에서 스폰/채택 완료
    private int _runId;

    // 외부(UIManager 등)
    public event Action<EquipmentManager> OnEquipmentReady;

    // --- 안전장치 캐시 ---
    private Transform _roomsRootCache;                                 // RoomsRoot 캐시(있을 때)
    private Transform _playerAnchor;                                   // 씬 루트용 안전 앵커

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        // 런타임 중 누가 부모를 RoomsRoot 쪽으로 옮겨도 즉시 탈착
        SafeDetachFromRoomsRoot();
    }

    /// <summary>다음 씬에서 "한 번" 깨끗하게 새로 스폰하자.</summary>
    public void RequestFreshSpawnNextScene()
    {
        _forceFreshSpawn = true;
        _spawnedThisScene = false;
        _runId++;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[PM] RequestFreshSpawnNextScene (runId={_runId})");
#endif
    }

    /// <summary>
    /// 재시작 전에 호출: 파괴 금지! 참조·플래그만 초기화.
    /// </summary>
    public void ResetState(bool hard = true)
    {
        _spawnedThisScene = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[PM] ResetState: refs/flags reset only. (no destroy)");
#endif
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != gameplaySceneName)
        {
            _spawnedThisScene = false;
            return;
        }

        // 씬별 안전 앵커/RoomsRoot 캐시 갱신
        RefreshSceneAnchors(scene);

        StartCoroutine(Co_SpawnAfterOneFrame(scene));
    }

    private System.Collections.IEnumerator Co_SpawnAfterOneFrame(Scene scene)
    {
        yield return null;
        TrySpawnOrAdoptInScene(scene);
    }

    private void TrySpawnOrAdoptInScene(Scene scene)
    {
        if (_spawnedThisScene) return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[PM] TrySpawnOrAdoptInScene: scene='{scene.name}', forceFresh={_forceFreshSpawn}");
#endif

        var existing = FindPlayersInScene(scene);

        if (existing.Count > 0)
        {
            if (existing.Count == 1)
            {
                Adopt(existing[0], scene);
                CompleteSpawn();
                return;
            }

            var keeper = existing[0];
            for (int i = 1; i < existing.Count; i++)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[PM] Extra Player purged: {existing[i].name}");
#endif
                // 부모 트리 최상단만 제거(여기선 keeper 외 중복만)
                Destroy(existing[i].transform.root.gameObject);
            }
            Adopt(keeper, scene);
            CompleteSpawn();
            return;
        }

        if (_forceFreshSpawn || Player == null || UnitRoot == null)
        {
            SpawnFresh(scene);
            CompleteSpawn();
            return;
        }

        MoveUnitRootToStartPoint();
        CompleteSpawn();
    }

    private void Adopt(PlayerController pc, Scene scene)
    {
        UnitRoot = pc.gameObject;
        Player = pc.transform.parent ? pc.transform.parent.gameObject : pc.gameObject;

        // ★ 항상 씬 루트(또는 앵커)로 강제 탈착
        HardReparentToAnchor(Player.transform, scene);

        // UnitRoot는 Player 자식으로 유지
        if (UnitRoot.transform.parent != Player.transform)
            UnitRoot.transform.SetParent(Player.transform, worldPositionStays: true);

        MoveUnitRootToStartPoint();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[PM] Adopted Player='{Player.name}', UnitRoot='{UnitRoot.name}'");
#endif
        RaiseEquipmentReadyIfPossible();
    }

    private void SpawnFresh(Scene scene)
    {
        var prefab = playerPrefab;
        if (prefab == null && !string.IsNullOrEmpty(playerResourcesPath))
            prefab = Resources.Load<GameObject>(playerResourcesPath);

        if (prefab == null)
        {
            Debug.LogError("[PM] playerPrefab missing.");
            return;
        }

        var go = Instantiate(prefab);

        // ★ 생성 즉시 씬 루트(또는 앵커)로 강제 배치
        HardReparentToAnchor(go.transform, scene);

        Player = go;

        var pc = go.GetComponentInChildren<PlayerController>(true);
        UnitRoot = pc ? pc.gameObject : go;

        MoveUnitRootToStartPoint();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[PM] Spawned '{Player.name}' (UnitRoot='{UnitRoot?.name}')");
#endif
        RaiseEquipmentReadyIfPossible();
    }

    private void CompleteSpawn()
    {
        _spawnedThisScene = true;
        _forceFreshSpawn = false;  // 이후 드래그&드랍, 수동 배치 불살
    }

    private void MoveUnitRootToStartPoint()
    {
        var rm = GameManager.Instance ? GameManager.Instance.RoomManager : null;
        if (rm != null && rm.HasStartPoint && UnitRoot != null)
            UnitRoot.transform.position = rm.GetStartPoint();
    }

    private void RaiseEquipmentReadyIfPossible()
    {
        if (UnitRoot == null) return;
        var eq = UnitRoot.GetComponent<EquipmentManager>();
        if (eq != null)
            OnEquipmentReady?.Invoke(eq);
    }

    // === deprecation-free: 현재 씬 한정 스캔(비활성 포함) ===
    private static List<PlayerController> FindPlayersInScene(Scene scene)
    {
        var all = UnityEngine.Object.FindObjectsByType<PlayerController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        var list = new List<PlayerController>(all.Length);
        for (int i = 0; i < all.Length; i++)
        {
            var pc = all[i];
            if (pc == null) continue;
            if (pc.gameObject.scene == scene) list.Add(pc);
        }
        return list;
    }

    // === 안전 앵커/RoomsRoot 캐시 & 방 트리 탈착 가드 =======================

    private void RefreshSceneAnchors(Scene scene)
    {
        _roomsRootCache = FindRoomsRoot(scene);

        // 전용 앵커가 없으면 만든다: 씬 루트에 고정
        _playerAnchor = FindOrCreatePlayerAnchor(scene);
    }

    private Transform FindRoomsRoot(Scene scene)
    {
        // 관례: 이름이 "RoomsRoot" 이거나 "roomsRoot" 같은 루트 오브젝트
        foreach (var go in scene.GetRootGameObjects())
        {
            if (go.name.Equals("RoomsRoot", StringComparison.OrdinalIgnoreCase))
                return go.transform;
        }
        return null;
    }

    private Transform FindOrCreatePlayerAnchor(Scene scene)
    {
        // 고정 이름의 앵커를 씬 루트에 유지
        const string AnchorName = "__PLAYER_ANCHOR__";
        foreach (var go in scene.GetRootGameObjects())
        {
            if (go.name == AnchorName) return go.transform;
        }
        var anchor = new GameObject(AnchorName);
        SceneManager.MoveGameObjectToScene(anchor, scene);
        return anchor.transform;
    }

    private void HardReparentToAnchor(Transform t, Scene scene)
    {
        if (t == null) return;
        // 먼저 씬 루트로 이동
        t.SetParent(null, worldPositionStays: true);
        SceneManager.MoveGameObjectToScene(t.gameObject, scene);

        // 그리고 앵커 밑에 둔다(방 트리 정리와 논리적으로 분리)
        if (_playerAnchor == null) _playerAnchor = FindOrCreatePlayerAnchor(scene);
        t.SetParent(_playerAnchor, worldPositionStays: true);
    }

    private void SafeDetachFromRoomsRoot()
    {
        if (Player == null) return;
        var pt = Player.transform;

        if (pt == null) return;

        // RoomsRoot 캐시가 없으면 시도해서 찾아본다(씬이 바뀐 직후 등)
        if (_roomsRootCache == null && Player.scene.IsValid())
            _roomsRootCache = FindRoomsRoot(Player.scene);

        if (_roomsRootCache == null) return;

        // Player가 RoomsRoot 하위에 들어가 있으면 즉시 탈착
        if (IsUnder(pt, _roomsRootCache))
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[PM] Detected Player under RoomsRoot → reparent to anchor.");
#endif
            HardReparentToAnchor(pt, Player.scene);
        }
    }

    private static bool IsUnder(Transform child, Transform root)
    {
        if (child == null || root == null) return false;
        var p = child.parent;
        while (p != null)
        {
            if (p == root) return true;
            p = p.parent;
        }
        return false;
    }
}
