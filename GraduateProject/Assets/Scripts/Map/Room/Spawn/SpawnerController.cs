using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;
using static Define;

[DisallowMultipleComponent]
public class SpawnerController : MonoBehaviour
{
    [Serializable]
    public class WeightedArchetype
    {
        public EnemyArchetypeSO archetype;
        [Range(0f, 1f)] public float weight = 0.25f;
    }

    [Header("Catalog Auto-Load")]
    public bool autoLoadCatalogByRoomType = true;
    [Tooltip("1순위 검색 경로 (Resources 내부)")]
    public string catalogResourcesPath = "SO/Enemies/Archetype";

    [Header("Spawn Candidates (Final)")]
    public List<WeightedArchetype> candidates = new();

    [Header("Spawn Counts")]
    public bool autoSpawnOnStart = false;
    public int minCount = 2;
    public int maxCount = 5;

    [Header("Behavior")]
    public bool allowMultipleSpawns = false;

    [Header("Spawn Area Detection")]
    public LayerMask groundMask;
    public LayerMask blockMask;
    public float separationRadius = 0.4f;
    public int maxSampleTry = 20;
    public float margin = 0.5f;
    public float groundRayDistance = 20f;

    [Header("Room Clear / Portal")]
    public bool autoTogglePortals = true;
    public string portalTag = "Portal";

    [Header("Legacy Enemies")]
    public bool disablePreplacedEnemies = true;
    public bool destroyPreplacedEnemies = true;

    [Header("Debug")]
    public bool debugLog = false;

    private float totalWeight;
    private readonly List<GameObject> spawned = new();
    private readonly HashSet<IHealth> healths = new();
    private GameObject[] portals;
    private Bounds roomBounds;
    private bool hasSpawned = false;

    // Tilemap 기반
    private Tilemap sourceTilemap;
    private readonly List<Vector3Int> seedCells = new();

    public event Action OnAllEnemiesDefeated;

    // ---- RoomGenerator 호환 ----
    public void Initialize(Tilemap tilemap, IList<Vector3Int> cells)
    {
        sourceTilemap = tilemap;
        seedCells.Clear();
        if (cells != null) seedCells.AddRange(cells);

        tilemap.CompressBounds();
        var lb = tilemap.localBounds;
        var worldCenter = tilemap.transform.TransformPoint(lb.center);
        var worldSize = Vector3.Scale(lb.size, tilemap.transform.lossyScale);
        roomBounds = new Bounds(worldCenter, worldSize);

        if (debugLog)
            Debug.Log($"[SpawnerController] Initialized from Tilemap. Bounds={roomBounds}, SeedCells={seedCells.Count}", this);
    }

    public void SetCandidates(EnemyArchetypeSO[] archetypes, float[] weights = null, float defaultW = 0.25f)
    {
        candidates.Clear();
        if (archetypes != null)
        {
            for (int i = 0; i < archetypes.Length; i++)
            {
                var a = archetypes[i];
                if (!a) continue;
                float w = (weights != null && i < weights.Length) ? Mathf.Clamp01(weights[i]) : defaultW;
                candidates.Add(new WeightedArchetype { archetype = a, weight = w });
            }
        }
        RecalcWeight();
    }

    public void SetCatalog(EnemyArchetypeCatalog catalog)
    {
        if (!catalog)
        {
            Debug.LogWarning("[SpawnerController] SetCatalog(null) ignored.");
            return;
        }
        candidates.Clear();
        foreach (var e in catalog.entries)
        {
            if (!e.archetype || e.weight <= 0f) continue;
            candidates.Add(new WeightedArchetype { archetype = e.archetype, weight = e.weight });
        }
        RecalcWeight();
    }

    private void Awake()
    {
        // 1) 카탈로그 자동 로드
        if (autoLoadCatalogByRoomType && candidates.Count == 0)
        {
            var room = GetComponent<Room>() ?? GetComponentInParent<Room>();
            var roomType = room ? room.Type : RoomType.Normal;

            // ★ 현재 스테이지 참조
            int stage = 1;
            var gm = GameManager.Instance;
            if (gm != null) stage = Mathf.Max(1, gm.currentStage);

            // Stage×RoomType 조합으로 카탈로그 조회
            var catalog = EnemyArchetypeRegistry.GetCatalog(
                stage, roomType,
                catalogResourcesPath,
                "SO/Stats/Enemies/Archetype",
                "" // 전체 스캔 폴백
            );

            if (catalog)
            {
                SetCatalog(catalog);
                if (debugLog) Debug.Log($"[SpawnerController] Catalog '{catalog.name}' loaded for Stage={stage}, RoomType={roomType}", this);
            }
            else
            {
                Debug.LogWarning($"[SpawnerController] No catalog found for Stage={stage}, RoomType={roomType}. " +
                                 $"Create one under Resources (e.g. '{catalogResourcesPath}'), or call SetCandidates()/SetCatalog().");
            }
        }

        // 2) Bounds/Portal 탐색
        if (roomBounds.size == Vector3.zero) CacheRoomBounds();
        if (autoTogglePortals) SafeFindPortals();
    }

    private void Start()
    {
        if (disablePreplacedEnemies) HandlePreplacedEnemies();
        if (autoTogglePortals) SetPortalsActive(false);
        if (autoSpawnOnStart) SpawnEnemies();
    }

    // ---- Room 공개 API ----
    public void SpawnEnemies()
    {
        if (hasSpawned && !allowMultipleSpawns)
        {
            if (debugLog) Debug.Log($"[SpawnerController] Spawn skipped (already spawned) on {name}");
            return;
        }
        if (candidates.Count == 0)
        {
            Debug.LogWarning("[SpawnerController] No candidates. Provide Catalog / SetCandidates() / autoLoadCatalogByRoomType.");
            return;
        }
        int count = Random.Range(minCount, maxCount + 1);
        SpawnWave(count);
        hasSpawned = true;
    }

    public bool AllEnemiesDefeated() => hasSpawned && healths.Count == 0;
    public int ActiveEnemyCount => healths.Count;

    // ---- 내부 구현 (이하 기존 그대로) ----
    private void RecalcWeight()
    {
        totalWeight = 0f;
        foreach (var c in candidates)
            if (c.archetype && c.weight > 0f)
                totalWeight += c.weight;
    }

    private void CacheRoomBounds()
    {
        var cols = GetComponentsInChildren<Collider2D>();
        if (cols != null && cols.Length > 0)
        {
            var b = new Bounds(cols[0].bounds.center, Vector3.zero);
            foreach (var c in cols) b.Encapsulate(c.bounds);
            roomBounds = b;
            return;
        }

        var rends = GetComponentsInChildren<Renderer>();
        if (rends != null && rends.Length > 0)
        {
            var b = new Bounds(rends[0].bounds.center, Vector3.zero);
            foreach (var r in rends) b.Encapsulate(r.bounds);
            roomBounds = b;
            return;
        }

        roomBounds = new Bounds(transform.position, new Vector3(6f, 4f, 0f));
    }

    private void SafeFindPortals()
    {
        var comps = GetComponentsInChildren<Portal>(true);
        if (comps != null && comps.Length > 0)
        {
            var list = new List<GameObject>(comps.Length);
            foreach (var c in comps) if (c) list.Add(c.gameObject);
            portals = list.ToArray();
            return;
        }

        if (!string.IsNullOrEmpty(portalTag))
        {
            try
            {
                var all = GameObject.FindGameObjectsWithTag(portalTag);
                var list = new List<GameObject>();
                foreach (var go in all)
                    if (go.transform.IsChildOf(transform)) list.Add(go);
                if (list.Count > 0) { portals = list.ToArray(); return; }
            }
            catch (UnityException) { }
        }

        {
            var list = new List<GameObject>();
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name.IndexOf("portal", StringComparison.OrdinalIgnoreCase) >= 0)
                    list.Add(t.gameObject);
            portals = list.ToArray();
        }
    }

    private void SetPortalsActive(bool active)
    {
        if (portals == null) SafeFindPortals();
        if (portals == null) return;
        foreach (var p in portals) if (p) p.SetActive(active);
    }

    private void HandlePreplacedEnemies()
    {
        var preplaced = GetComponentsInChildren<EnemyController>(true);
        foreach (var ec in preplaced)
        {
            if (ec.TryGetComponent<SpawnedEnemyTag>(out _)) continue;
            if (destroyPreplacedEnemies) Destroy(ec.gameObject);
            else ec.gameObject.SetActive(false);
            if (debugLog) Debug.Log($"[SpawnerController] Preplaced Enemy removed: {ec.name}", ec);
        }
    }

    private void SpawnWave(int n)
    {
        for (int i = 0; i < n; i++)
        {
            var arch = Pick();
            if (!arch || !arch.prefab)
            {
                Debug.LogWarning("[SpawnerController] Invalid archetype/prefab.");
                continue;
            }

            Vector3 pos = SampleSpawnPoint();
            var go = Instantiate(arch.prefab, pos, Quaternion.identity, transform);
            go.name = $"{arch.kind}_{i}";

            var compRoot = FindComponentRoot(go.transform);
            var rootGO = compRoot ? compRoot.gameObject : go;

            var tag = rootGO.GetComponent<SpawnedEnemyTag>() ?? rootGO.AddComponent<SpawnedEnemyTag>();
            tag.SourceSpawner = this;

            var assembler = rootGO.GetComponent<EnemyAssembler>() ?? rootGO.AddComponent<EnemyAssembler>();
            assembler.Setup(arch, compRoot);

            spawned.Add(rootGO);

            IHealth hp = rootGO.GetComponent<IHealth>();
            if (hp == null) hp = rootGO.GetComponentInChildren<IHealth>(true);

            if (hp != null)
            {
                hp.OnDead += () => OnEnemyDead(rootGO, hp);
                healths.Add(hp);
            }
            else
            {
                Debug.LogWarning($"[SpawnerController] Spawned enemy has no IHealth: {rootGO.name}", rootGO);
            }
        }
    }

    private void OnEnemyDead(GameObject go, IHealth hp)
    {
        healths.Remove(hp);
        if (debugLog) Debug.Log($"[SpawnerController] Enemy died: {go.name}. Left: {healths.Count}", go);

        if (healths.Count == 0)
        {
            if (debugLog) Debug.Log($"[SpawnerController] ROOM CLEARED! ({name})");
            if (autoTogglePortals) SetPortalsActive(true);
            OnAllEnemiesDefeated?.Invoke();
        }
    }

    private EnemyArchetypeSO Pick()
    {
        if (totalWeight <= 0f) RecalcWeight();
        if (totalWeight <= 0f || candidates.Count == 0) return null;

        float roll = Random.value * totalWeight;
        float acc = 0f;
        foreach (var c in candidates)
        {
            if (!c.archetype || c.weight <= 0f) continue;
            acc += c.weight;
            if (roll <= acc) return c.archetype;
        }
        return candidates[^1].archetype;
    }

    private Vector3 SampleSpawnPoint()
    {
        if (sourceTilemap && seedCells.Count > 0)
        {
            int idx = Random.Range(0, seedCells.Count);
            var cell = seedCells[idx];
            seedCells.RemoveAt(idx);

            var center = (Vector2)sourceTilemap.GetCellCenterWorld(cell);
            float topY = roomBounds.max.y + 0.5f;
            var hit = Physics2D.Raycast(new Vector2(center.x, topY), Vector2.down, groundRayDistance, groundMask);
            Vector2 p = hit.collider ? hit.point : center;

            bool blocked = Physics2D.OverlapCircle(p, separationRadius, blockMask);
            if (!blocked) return new Vector3(p.x, p.y + 0.01f, 0f);
        }

        var b = roomBounds;
        float minX = b.min.x + margin;
        float maxX = b.max.x - margin;
        float top = b.max.y + 0.5f;

        for (int i = 0; i < maxSampleTry; i++)
        {
            float x = Random.Range(minX, maxX);
            var hit = Physics2D.Raycast(new Vector2(x, top), Vector2.down, groundRayDistance, groundMask);
            if (hit.collider != null)
            {
                Vector2 p = hit.point;
                bool blocked = Physics2D.OverlapCircle(p, separationRadius, blockMask);
                if (!blocked) return new Vector3(p.x, p.y + 0.01f, 0f);
            }
        }

        if (debugLog) Debug.LogWarning("[SpawnerController] Failed to sample spawn point. Using room center.");
        return b.center;
    }

    private Transform FindComponentRoot(Transform instRoot)
    {
        foreach (var t in instRoot.GetComponentsInChildren<Transform>(true))
            if (t.name.Equals("UnitRoot", StringComparison.OrdinalIgnoreCase))
                return t;

        var monos = instRoot.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var m in monos)
        {
            if (!m) continue;
            if (m is ICombatStatHolder || m.GetComponent<StatController>())
                return m.transform;
        }

        foreach (var m in monos)
        {
            if (!m) continue;
            if (m.GetComponent<Rigidbody2D>() && m.GetComponent<Collider2D>())
                return m.transform;
        }
        return instRoot;
    }
}
