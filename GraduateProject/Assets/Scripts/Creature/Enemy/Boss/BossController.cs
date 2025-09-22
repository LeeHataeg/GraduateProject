using UnityEngine;
using BossState = Define.BossState;
using AnimKey = Define.AnimKey;
using System.Collections;
[RequireComponent(typeof(HealthController))]
[RequireComponent(typeof(Rigidbody2D))]
public class BossController : MonoBehaviour
{
    [Header("Data")]
    public BossDefinitionSO def;

    [Header("Scene Refs")]
    [Tooltip("공격/스폰 기준점(없으면 자기 transform)")]
    public Transform attackOrigin;
    [Tooltip("타겟(초기엔 PlayerManager에서 찾아 넣거나, 에디터로 할당)")]
    public Transform player;

    [Header("Optional")]
    public bool autoFindPlayer = true;

    // internals
    private HealthController hp;
    private Rigidbody2D rb;
    private IAnimationController anim;
    private BossState state;
    private BossPhaseSO curPhase;
    private Coroutine running;
    private bool moveLocked;
    private bool invulnerable;

    private BossContext Ctx => new BossContext
    {
        Self = transform,
        AttackOrigin = attackOrigin ? attackOrigin : transform,
        Player = player,
        Anim = anim,
        Anims = def ? def.animMap : null,
        Health = hp,
        RB = rb,
        LockMove = LockMove
    };

    void Awake()
    {
        hp = GetComponent<HealthController>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<IAnimationController>();

        if (def == null) Debug.LogError("[BossController] Definition SO 미할당!");
        if (def && def.animMap == null) Debug.LogError("[BossController] AnimMapSO 미할당!");
        if (def) def.animMap?.Build();
    }

    void Start()
    {
        if (autoFindPlayer && player == null)
        {
            var pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj) player = pObj.transform;
        }

        hp.OnDead += OnDead;
        EnterIntro();
    }

    void Update()
    {
        if (state == BossState.Phase1 || state == BossState.Phase2)
        {
            // Phase 전환 체크(Phase1 한정)
            if (state == BossState.Phase1 && def && def.phase1)
            {
                float gate = def.phase1.toNextPhaseHpRate;
                if (gate > 0f && hp.CurrentHp <= hp.Stats.MaxHp * gate)
                {
                    StartCoroutine(GoTransition());
                    return;
                }
            }
            if (running == null) running = StartCoroutine(PhaseLoop());
        }

        // 간단 추격(이동락 중이면 정지)
        if (!moveLocked && player)
        {
            var v = rb.linearVelocity;
            v.x = Mathf.Sign(player.position.x - transform.position.x) * 2.5f; // 간단 추격속도
            rb.linearVelocity = v;
        }
    }

    void EnterIntro()
    {
        state = BossState.Intro;
        curPhase = def.phase1;
        def.animMap.Play(anim, AnimKey.EntryP1);
        StartCoroutine(BeginPhase1AfterEntry());
    }

    IEnumerator BeginPhase1AfterEntry()
    {
        // 실제론 애니메이션 이벤트로 타이밍 맞추는 걸 권장.
        yield return new WaitForSeconds(1.6f);
        def.animMap.Play(anim, AnimKey.Taunt);
        yield return new WaitForSeconds(0.8f);
        state = BossState.Phase1;
        if (curPhase) def.animMap.Play(anim, curPhase.onEnterPlay);
    }

    IEnumerator PhaseLoop()
    {
        while (state == BossState.Phase1 || state == BossState.Phase2)
        {
            var move = curPhase ? curPhase.Pick(Ctx) : null;
            if (move != null)
            {
                yield return StartCoroutine(move.Run(Ctx));
            }
            else
            {
                // 기본 유휴(워킹 토글)
                def.animMap.Play(anim, AnimKey.Walking, true);
                yield return new WaitForSeconds(0.25f);
            }
        }
        running = null;
    }

    IEnumerator GoTransition()
    {
        state = BossState.Transition;
        running = null;

        def.animMap.Play(anim, AnimKey.Fall);
        yield return new WaitForSeconds(0.6f);
        def.animMap.Play(anim, AnimKey.Fallen);
        yield return new WaitForSeconds(0.3f);

        curPhase = def.phase2;
        state = BossState.Phase2;
        def.animMap.Play(anim, AnimKey.EntryP2);
        yield return new WaitForSeconds(0.8f);
        if (curPhase) def.animMap.Play(anim, curPhase.onEnterPlay);
    }

    void OnDead()
    {
        if (state == BossState.Death) return;
        state = BossState.Death;
        StopAllCoroutines();
        def.animMap.Play(anim, def.deathKey);
        LockMove(true);
        StartCoroutine(DoFadeAndClear());
    }

    IEnumerator DoFadeAndClear()
    {
        yield return new WaitForSeconds(def.fadeDelay);
        def.animMap.Play(anim, def.fadeKey);

        // 룸 클리어/포탈 오픈 신호 — 프로젝트 룸 시스템에 맞게 메서드명만 바꿔줘
        // RoomManager.Instance?.OnBossCleared();

        Destroy(gameObject, 2f);
    }

    // -------- 이동/무적 보조 --------
    void LockMove(bool on)
    {
        moveLocked = on;
        if (on)
        {
            var v = rb.linearVelocity;
            v.x = 0f;
            rb.linearVelocity = v;
        }
    }

    // -------- 애니메이션 이벤트 콜백(원하는 것만 사용) --------
    public void OnAE_Hit() { /* 필요 시, 현재 수행 중 Move에 위임 */ }
    public void OnAE_Spawn(string id) { /* id별 프리팹 스폰 라우팅 */ }
    public void OnAE_Invuln(bool on) { invulnerable = on; }
    public void OnAE_PhaseGate() { /* Entry → 전투 시작 같은 타이밍 제어 */ }

    // 외부에서 참조 편의를 위해 노출
    public bool IsInvulnerable => invulnerable;
}