using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class SpawnerController : MonoBehaviour
{
    private Tilemap _tilemap;
    private List<Vector3Int> _spawnCells;

    public event Action AllEnemiesDefeated;

    private bool _hasSpawned;
    private int _alive;

    [Header("Optional Cleanup")]
    [SerializeField] private bool cleanupRootOnDeath = true;

    public void Initialize(Tilemap tilemap, List<Vector3Int> spawnCells)
    {
        _tilemap = tilemap;
        _spawnCells = spawnCells;
        _hasSpawned = false;
        _alive = 0;
    }

    public void SpawnEnemies()
    {
        if (_hasSpawned) return;
        _hasSpawned = true;

        var prefab = Resources.Load<GameObject>("Prefabs/Enemies/Enemy_Skul_Normal");
        if (prefab == null)
        {
            Debug.LogError("[Spawner] Enemy 프리팹 로드 실패");
            return;
        }

        _alive = 0;

        foreach (var cell in _spawnCells)
        {
            Vector3 worldPos = _tilemap.CellToWorld(cell) + _tilemap.cellSize * 0.5f;

            var root = Instantiate(prefab, worldPos, Quaternion.identity, transform);

            // *** 핵심: 실제로 Destroy되는 'DeathCarrier'를 찾아 그곳에 Hook을 단다 ***
            var deathCarrier = FindDeathCarrier(root);
            if (deathCarrier == null)
            {
                // 최후의 보루: 그래도 못 찾으면 그냥 root에 단다(비추지만 동작은 하게).
                Debug.LogWarning("[Spawner] DeathCarrier를 찾지 못해 root에 Hook 부착");
                deathCarrier = root;
            }

            var hook = deathCarrier.AddComponent<SpawnedEnemyHook>();
            hook.OnDied += OnEnemyDied;

            // 죽을 때 상위 쉘까지 지우고 싶으면 여기에 참조 넘겨서 정리
            hook.RootToCleanup = cleanupRootOnDeath ? root : null;

            _alive++;
        }

        Debug.Log($"[Spawner] Spawn {_alive} enemies");
    }

    // SPUM 구조 대응: 이름에 UnitRoot가 있거나 실제 본체로 보이는 자식 찾기
    private GameObject FindDeathCarrier(GameObject root)
    {
        // 1) 이름 정확/부분 일치 시도
        var t = root.transform.Find("UnitRoot");
        if (t != null) return t.gameObject;

        // 2) 대소문자 무시 부분 검색
        foreach (var tr in root.GetComponentsInChildren<Transform>(true))
        {
            if (tr.name.Equals("UnitRoot", StringComparison.OrdinalIgnoreCase)) return tr.gameObject;
            if (tr.name.IndexOf("UnitRoot", StringComparison.OrdinalIgnoreCase) >= 0) return tr.gameObject;
        }

        // 3) 대안: Animator가 붙은 가장 깊은 자식(종종 본체)
        Animator deepestAnim = null;
        foreach (var anim in root.GetComponentsInChildren<Animator>(true))
        {
            // 가장 깊어 보이는 걸로(자식 수가 많거나 부모에서 멀수록)
            if (deepestAnim == null || anim.transform.GetComponentsInParent<Transform>(true).Length >
                                       deepestAnim.transform.GetComponentsInParent<Transform>(true).Length)
            {
                deepestAnim = anim;
            }
        }
        if (deepestAnim != null) return deepestAnim.gameObject;

        // 4) 정말 없으면 첫 번째 활성 자식
        if (root.transform.childCount > 0)
            return root.transform.GetChild(0).gameObject;

        // 실패
        return null;
    }

    private void OnEnemyDied(SpawnedEnemyHook hook)
    {
        if (hook != null) hook.OnDied -= OnEnemyDied;

        _alive = Mathf.Max(0, _alive - 1);
        Debug.Log($"[Spawner] Enemy died. Alive: {_alive}");

        // 남은 쉘 정리(선택)
        if (hook != null && hook.RootToCleanup != null)
        {
            Destroy(hook.RootToCleanup);
        }

        if (_alive == 0)
        {
            Debug.Log("[Spawner] All enemies defeated!");
            AllEnemiesDefeated?.Invoke();
        }
    }
}

// *** 새/수정된 Hook: '죽는 그 오브젝트'에 붙는다 ***
public class SpawnedEnemyHook : MonoBehaviour
{
    public event Action<SpawnedEnemyHook> OnDied;

    // 선택: 부모 쉘 컨테이너까지 치울 때 사용
    public GameObject RootToCleanup;

    private bool _fired;

    private void OnDestroy()
    {
        // SPUM 적사망: UnitRoot(혹은 본체)가 Destroy될 때 여기로 들어온다
        if (_fired) return;
        _fired = true;
        OnDied?.Invoke(this);
    }

    // 혹시 죽을 때 Destroy 대신 Disable 처리하는 적이 있다면 대비용(선택)
    private void OnDisable()
    {
        // Destroy가 아닌 단순 Disable만 쓰는 적이 있다면 주석 해제 고려
        // if (_fired) return;
        // _fired = true;
        // OnDied?.Invoke(this);
    }
}
