using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stage1 보스 전용 "죽음 후 처리" 오케스트레이터.
/// - HealthController.OnDead 구독
/// - 즉시 AI/공격/충돌 정지 + Animator에 isDeath=true
/// - delay 후 보스 오브젝트 Destroy
/// - 다음 스테이지로 가는 포탈 스폰 (GM.StagePortalPrefab 우선, 없으면 Resources 폴백)
/// </summary>
[DisallowMultipleComponent]
public class Stage1BossDeathOrchestrator : MonoBehaviour
{
    [Header("Death Flow")]
    [Tooltip("사망 애니메이션 재생 후 보스 제거까지 대기 시간(초)")]
    public float destroyDelay = 3.0f;

    [Tooltip("사망 직후 물리/이동을 완전히 고정할지 여부")]
    public bool freezeRigidBody = true;

    [Tooltip("사망 직후 모든 Collider2D를 꺼서 충돌을 차단할지 여부")]
    public bool disableAllCollidersOnDeath = true;

    [Header("Animator")]
    [Tooltip("Animator에 존재하는 사망 bool 파라미터명(없으면 비워둬도 됨)")]
    public string isDeathBoolParam = "isDeath";
    [Tooltip("사망 직후 리셋할 수 있는 Trigger 목록(선택)")]
    public string[] triggersToReset = { "ToJump", "LightAtk", "FrontHeavyAtk", "2_Attack", "3_Damaged", "4_Death" };

    [Header("Portal Spawn")]
    [Tooltip("GameManager.Instance.StagePortalPrefab이 비어있을 경우 Resources에서 폴백 로드")]
    public string resourcesPortalPath = "Maps/Portal/Portal_Purple"; // Resources/Maps/Portal/Portal_Purple
    [Tooltip("포탈 스폰 우선 위치 (비우면 자동 탐색)")]
    public Transform portalSpawnPointOverride;

    [Tooltip("보스가 속한 방(포탈을 이 아래에 자식으로 생성). 비우면 보스 부모 또는 RoomsRoot에 붙임")]
    public Transform portalParentOverride;

    [Tooltip("포탈 프리팹에 StageTransitionPortal/Portal 등이 달려있다는 전제(상호작용으로 다음 스테이지 이동)")]
    public bool autoActivatePortal = true; // 일반적으로 프리팹 로직이 처리함

    [Header("Debug")]
    public bool log = false;

    private bool _handled;
    private Animator _anim;
    private Rigidbody2D _rb;
    private HealthController _hp;

    private void Awake()
    {
        _hp = GetComponentInChildren<HealthController>();
        _anim = GetComponentInChildren<Animator>();
        _rb = GetComponentInChildren<Rigidbody2D>();

        if (_hp == null)
        {
            Debug.LogError("[Stage1BossDeathOrchestrator] HealthController not found.", this);
            enabled = false;
            return;
        }
        _hp.OnDead += OnBossDead;
    }

    private void OnDestroy()
    {
        if (_hp != null) _hp.OnDead -= OnBossDead;
    }

    private void OnBossDead()
    {
        if (_handled) return;
        _handled = true;

        if (log) Debug.Log("[Stage1BossDeathOrchestrator] Boss Dead → halting AI/attacks, arming death anim.", this);

        // 1) 즉시 AI/공격 정지
        HaltAllBehaviours();

        // 2) Animator: isDeath=true 및 잡다한 트리거 리셋
        if (_anim)
        {
            foreach (var tr in triggersToReset)
            {
                if (!string.IsNullOrEmpty(tr)) _anim.ResetTrigger(tr);
            }
            if (!string.IsNullOrEmpty(isDeathBoolParam))
                _anim.SetBool(isDeathBoolParam, true);
        }

        // 3) 물리/충돌 정지
        if (freezeRigidBody && _rb)
        {
#if UNITY_6000_0_OR_NEWER
            _rb.linearVelocity = Vector2.zero;
#else
            _rb.velocity = Vector2.zero;
#endif
            _rb.angularVelocity = 0f;
            _rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }
        if (disableAllCollidersOnDeath)
        {
            foreach (var col in GetComponentsInChildren<Collider2D>(true))
                col.enabled = false;
        }

        // 4) 포탈 스폰 (즉시)
        TrySpawnStagePortal();

        // 5) 일정 시간 후 보스 제거
        StartCoroutine(Co_DestroyAfterDelay());
    }

    private IEnumerator Co_DestroyAfterDelay()
    {
        if (destroyDelay > 0f)
            yield return new WaitForSeconds(destroyDelay);

        if (log) Debug.Log("[Stage1BossDeathOrchestrator] Destroy boss object", this);
        Destroy(gameObject);
    }

    private void HaltAllBehaviours()
    {
        // 공격/AI/이동 등 모두 꺼버린다. (Stage2 보스에 영향 없는 "보스 프리팹 내부"만)
        // 필요 시 여기서 프로젝트 내 구체 스크립트명 추가
        var monos = GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var m in monos)
        {
            if (!m) continue;

            // Animator/이 스크립트/HealthController만 유지
            if (m == this) continue;
            if (m == _hp) continue;
            if (m == _anim) continue;

            // 시각 효과 전용 등은 켜두고 싶다면 예외 처리 가능

            // 대표적으로 AI/컨트롤러/히트박스/무기 등은 정지
            // (이름 기준으로 안전하게 대상 좁히기)
            var n = m.GetType().Name;
            if (n.Contains("Controller") || n.Contains("Driver") || n.Contains("Behavior") || n.Contains("Attack") || n.Contains("Spawner"))
            {
                m.enabled = false;
            }
        }
    }

    private void TrySpawnStagePortal()
    {
        GameObject prefab = null;

        // 1) GM 설정 우선
        var gm = GameManager.Instance;
        if (gm != null && gm.StagePortalPrefab != null)
            prefab = gm.StagePortalPrefab;

        // 2) 폴백: Resources 경로
        if (prefab == null)
            prefab = Resources.Load<GameObject>(resourcesPortalPath);

        if (prefab == null)
        {
            Debug.LogError("[Stage1BossDeathOrchestrator] Stage portal prefab not found. Check GameManager.StagePortalPrefab or Resources path.", this);
            return;
        }

        // 부모 결정: 우선 지정 → 보스 부모 → RoomsRoot(Grid)
        Transform parent = portalParentOverride;
        if (!parent)
        {
            parent = transform.parent;
            if (!parent)
            {
                var rm = gm != null ? gm.RoomManager : null;
                if (rm != null && rm.Grid != null) parent = rm.Grid.transform;
            }
        }

        // 스폰 위치 결정: 우선 지정 → "PortalSpawn" 이름/태그 → 보스 위치
        Vector3 pos = transform.position;
        if (portalSpawnPointOverride)
            pos = portalSpawnPointOverride.position;
        else
        {
            // 이름/태그로 자동 탐색
            Transform found = null;
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name.Equals("PortalSpawn", System.StringComparison.OrdinalIgnoreCase) ||
                    t.name.Equals("StagePortalSpawn", System.StringComparison.OrdinalIgnoreCase) ||
                    t.CompareTag("PortalSpawn"))
                {
                    found = t; break;
                }
            }
            if (found) pos = found.position;
        }

        var go = Instantiate(prefab, pos, Quaternion.identity, parent);
        go.name = "Portal_Purple (NextStage)";

        if (log) Debug.Log($"[Stage1BossDeathOrchestrator] Spawned stage portal at {pos}, parent={parent?.name}", this);

        // 일반적으로 포탈 프리팹이 자체적으로 동작(트리거/OnInteract)하지만,
        // 필요 시 여기서 바로 초기화해줄 수 있음.
        // (예: StageTransitionPortal 같은 스크립트가 있으면 자동으로 동작)
    }
}
