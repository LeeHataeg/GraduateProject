using static Define;
using UnityEngine;
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
    [Tooltip("타겟(초기에 Tag=Player 탐색 가능)")]
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

        if (!def) Debug.LogError("[BossController] Definition SO 미할당!");
        if (def && !def.animMap) Debug.LogError("[BossController] AnimMapSO 미할당!");
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
                float gate = Mathf.Clamp01(def.phase1.toNextPhaseHpRate);
                if (gate > 0f && hp.CurrentHp <= hp.MaxHp * gate)
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
            v.x = Mathf.Sign(player.position.x - transform.position.x) * 2.5f;
            rb.linearVelocity = v;

            // 바라보는 방향 보정(스프라이트 좌우 반전)
            if (v.x != 0f)
            {
                var ls = transform.localScale;
                ls.x = Mathf.Abs(ls.x) * (v.x > 0 ? -1 : 1); // 스프라이트 기본이 왼쪽 바라볼 때 기준
                transform.localScale = ls;
            }
        }
    }

    void EnterIntro()
    {
        state = BossState.Intro;
        curPhase = def.phase1;

        // 네가 정한 P1 시작 시퀀스
        StartCoroutine(BeginPhase1AfterEntry());
    }

    IEnumerator BeginPhase1AfterEntry()
    {
        def.animMap.Play(anim, AnimKey.Entry2);
        yield return new WaitForSeconds(0.6f);

        def.animMap.Play(anim, AnimKey.Falling);
        yield return new WaitForSeconds(0.4f);

        def.animMap.Play(anim, AnimKey.Land);
        yield return new WaitForSeconds(0.3f);

        def.animMap.Play(anim, AnimKey.Taunt);
        yield return new WaitForSeconds(0.8f);

        def.animMap.Play(anim, AnimKey.TauntOut);
        yield return new WaitForSeconds(0.4f);

        state = BossState.Phase1;
        def.animMap.Play(anim, AnimKey.Idle);
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
                // 아무 것도 못 골랐을 때: 워킹 루프 잠깐
                def.animMap.Play(anim, AnimKey.Walking, true);
                yield return new WaitForSeconds(0.25f);
                def.animMap.Play(anim, AnimKey.Walking, false);
            }
        }
        running = null;
    }

    IEnumerator GoTransition()
    {
        state = BossState.Transition;
        running = null;

        def.animMap.Play(anim, AnimKey.Fall);
        yield return new WaitForSeconds(0.4f);

        def.animMap.Play(anim, AnimKey.Fallen);
        yield return new WaitForSeconds(0.4f);

        curPhase = def.phase2;
        state = BossState.Phase2;

        def.animMap.Play(anim, AnimKey.Entry2);
        yield return new WaitForSeconds(0.7f);

        def.animMap.Play(anim, AnimKey.Idle2);
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

        // 룸/포탈 신호가 필요하면 여기서 호출:
        // RoomManager.Instance?.OnBossCleared();

        Destroy(gameObject, 2f);
    }

    // -------- 이동/무적 보조 --------
    void LockMove(bool on)
    {
        moveLocked = on;
        if (on)
        {
            var v = rb.linearVelocity; v.x = 0f; rb.linearVelocity = v;
        }
    }

    // -------- 애니메이션 이벤트 콜백 --------
    public void OnAE_Hit() { /* 현재 무브에 위임하고 싶으면 확장 */ }
    public void OnAE_Spawn(string id) { /* id별 프리팹 스폰 라우팅 */ }
    public void OnAE_Invuln(bool on) { invulnerable = on; }
    public void OnAE_PhaseGate() { /* 필요 시 전투개시 등 트리거 */ }

    public bool IsInvulnerable => invulnerable;
}