using UnityEngine;
using System.Collections;
using System;
using BossState = Define.BossState;
using AnimKey = Define.AnimKey;
using System.Collections.Generic;

[RequireComponent(typeof(HealthController))]
[RequireComponent(typeof(Rigidbody2D))]
public class BossController : MonoBehaviour
{
    [Header("Data")]
    public BossDefinitionSO def;

    [Header("Scene Refs")]
    [Tooltip("공격/스폰 기준점(없으면 자기 transform)")]
    public Transform attackOrigin;
    [Tooltip("타겟(초기엔 Player 태그 검색)")]
    public GameObject UnitRoot;

    [Header("Optional")]
    public bool autoFindPlayer = true;

    [Header("Facing")]
    [Tooltip("에셋 기본 바라보는 방향: 오른쪽이면 true, 왼쪽이면 false")]
    public bool spriteFacesRight = true;
    [SerializeField] private SpriteRenderer sr;

    [Header("Locomotion")]
    [SerializeField] private float moveSpeed = 6.0f;   // X 추격 속도
    [SerializeField] private float accel = 20.0f;      // 가속/감속
    [SerializeField] private float stopDist = 2.5f;    // 추격 중단 거리(Animator SpeedBlend 임계와 맞추면 자연스러움)

    [Header("Visual Root / RB Guard (추가)")]
    [SerializeField] private Transform visualRoot;     // 스프라이트/애니 루트(없으면 transform)
    [SerializeField] private bool forceUnfreezeX = true;  // 실행 중 X 프리즈 방지
    [SerializeField] private bool forceDynamicRB = true;  // 실행 중 Dynamic 강제

    // internals
    private HealthController hp;
    private Rigidbody2D rb;
    private IAnimationController anim;   // AnimatorAdaptor
    private Animator unityAnimator;      // 파라미터/Trigger 직접 접근
    private BossState state;
    private BossPhaseSO curPhase;
    private Coroutine coPhase;
    private Coroutine coTransition;
    private HashSet<int> _animParamSet;

    private bool moveLocked = true;  // 시작은 Intro 잠금
    private bool invulnerable;
    private bool casting;            // 공격 시전 중 강제 이동락

    public bool IsInvulnerable => invulnerable;

    // Intro 플래그
    private bool entryLanded;  // Ground 접촉(EndFalling 트리거 발사됨)
    private bool entrySettled; // Idle 도달 및 Phase1 시작 가능

    // Debug
    public float deltaX;

    // ==== Animator Param/Trigger Hash ====
    private static readonly int H_IsPhase2 = Animator.StringToHash("IsPhase2");
    private static readonly int H_IsDead = Animator.StringToHash("IsDead");
    private static readonly int H_IsStun = Animator.StringToHash("IsStun"); // 외부 시스템 연동용(필요 시)
    private static readonly int H_SpeedBlend = Animator.StringToHash("SpeedBlend");
    private static readonly int H_MoveX = Animator.StringToHash("MoveX");

    // Intro 종료 트리거(이름 2종 호환)
    private static readonly int T_EndFalling = Animator.StringToHash("EndFalling");
    private static readonly int T_IsEndFallingAlt = Animator.StringToHash("IsEndFalling");

    private BossContext Ctx => new BossContext
    {
        Self = transform,
        AttackOrigin = attackOrigin ? attackOrigin : transform,
        Player = UnitRoot ? UnitRoot.transform : null,
        Anim = anim,
        Anims = def ? def.animMap : null,
        Health = hp,
        RB = rb,
        LockMove = LockMove
    };

    private void Awake()
    {
        hp = GetComponent<HealthController>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<IAnimationController>();
        unityAnimator = GetComponent<Animator>();
        if (!sr) sr = GetComponentInChildren<SpriteRenderer>();
        if (!attackOrigin) attackOrigin = transform;
        if (!visualRoot) visualRoot = transform;

        if (unityAnimator) unityAnimator.applyRootMotion = false; // 2D 이동은 스크립트/물리로

        if (!def) Debug.LogError("[BossController] Definition SO 미할당!");
        def?.animMap?.Build();
        BuildAnimParamCache();
        ConfigureRB();
    }

    private void ConfigureRB()
    {
        if (!rb) return;

        if (forceDynamicRB) rb.bodyType = RigidbodyType2D.Dynamic; // Kinematic/Static이면 이동 안 함
        rb.simulated = true;
        rb.freezeRotation = true;

        if (forceUnfreezeX)
        {
            // X 프리즈가 잡혀 있으면 해제
            rb.constraints &= ~RigidbodyConstraints2D.FreezePositionX;
        }
    }

    private void Start()
    {
        if (autoFindPlayer && UnitRoot == null)
            UnitRoot = GameObject.FindGameObjectWithTag("Player");

        if (hp != null) hp.OnDead += HandleDead;

        EnterIntro();
    }

    private void OnDestroy()
    {
        if (hp != null) hp.OnDead -= HandleDead;
    }

#if UNITY_EDITOR
    private float _logTimer;
    private void Update()
    {
        _logTimer += Time.unscaledDeltaTime;
        if (_logTimer > 1f)
        {
            _logTimer = 0f;
            var clip = anim != null ? anim.GetCurClipname() : "(null)";
            Debug.Log($"[Boss] state={state} locked={moveLocked} casting={casting} clip={clip}");
        }
    }
#endif

    private void FixedUpdate()
    {
        // ===== Animator 파라미터 동기화 =====
        if (unityAnimator)
        {
            bool isPhase2 = (state == BossState.Phase2);
            unityAnimator.SetBool(H_IsPhase2, isPhase2);
            unityAnimator.SetBool(H_IsDead, state == BossState.Death);

            float dist = 0f;
            float moveX = 0f;
            if (UnitRoot)
            {
                var p = UnitRoot.transform.position;
                dist = Vector2.Distance(p, transform.position);
                moveX = Mathf.Clamp(p.x - transform.position.x, -1f, 1f);
            }
            unityAnimator.SetFloat(H_SpeedBlend, dist);
            unityAnimator.SetFloat(H_MoveX, moveX);
        }

        // ===== 상태 머신 =====
        if (state == BossState.Intro)
        {
            def?.animMap?.Play(anim, AnimKey.Falling, true); // Falling 유지

            if (entryLanded && IsInIdle())
            {
                entrySettled = true;
                moveLocked = false;           // 이동 해제
                state = BossState.Phase1;
                curPhase = def ? def.phase1 : null;
                EnsurePhaseLoop();
            }
            return;
        }

        if (state == BossState.Transition || state == BossState.Death)
            return;

        if (state == BossState.Phase1 || state == BossState.Phase2)
        {
            // HP 게이트(Phase2 전환)
            if (state == BossState.Phase1 && def && def.phase1 != null && hp != null)
            {
                float gate = def.phase1.toNextPhaseHpRate;
                float maxHp = hp.Stats != null ? hp.Stats.MaxHp : Mathf.Max(1, hp.MaxHp);
                if (gate > 0f && hp.CurrentHp <= maxHp * gate)
                {
                    StopPhaseLoop();
                    StartTransitionToPhase2();
                    return;
                }
            }

            EnsurePhaseLoop();
            HandleLocomotion(); // 실제 이동/바라보기
        }
    }

    // === 이동/바라보기 전담 ===
    private void HandleLocomotion()
    {
        if (moveLocked || casting) { HaltHorizontal(); return; }
        if (!rb || !UnitRoot) { HaltHorizontal(); return; }

        float dx = UnitRoot.transform.position.x - transform.position.x;
        float adx = Mathf.Abs(dx);
        deltaX = dx;

        FaceToX(dx);

        if (adx <= stopDist)
        {
            // 감속하여 정지
#if UNITY_6000_0_OR_NEWER
            var v = rb.linearVelocity;
#else
            var v = rb.velocity;
#endif
            v.x = Mathf.MoveTowards(v.x, 0f, accel * Time.fixedDeltaTime);
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = v;
#else
            rb.velocity = v;
#endif
            return;
        }

        // 추격(가속)
#if UNITY_6000_0_OR_NEWER
        var vel = rb.linearVelocity;
#else
        var vel = rb.velocity;
#endif
        float targetVX = Mathf.Sign(dx) * moveSpeed;
        vel.x = Mathf.MoveTowards(vel.x, targetVX, accel * Time.fixedDeltaTime);
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = vel;
#else
        rb.velocity = vel;
#endif
    }

    private void HaltHorizontal()
    {
        if (!rb) return;
#if UNITY_6000_0_OR_NEWER
        var v = rb.linearVelocity;
        if (Mathf.Abs(v.x) > 0.001f)
        {
            v.x = Mathf.MoveTowards(v.x, 0f, accel * Time.fixedDeltaTime);
            rb.linearVelocity = v;
        }
#else
        var v = rb.velocity;
        if (Mathf.Abs(v.x) > 0.001f)
        {
            v.x = Mathf.MoveTowards(v.x, 0f, accel * Time.fixedDeltaTime);
            rb.velocity = v;
        }
#endif
    }

    // === 충돌: Intro 낙하 종료 트리거 ===
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null) return;

        if (state == BossState.Intro && moveLocked && collision.gameObject.CompareTag("Ground"))
        {
            bool fired = false;
            // "EndFalling" 또는 "IsEndFalling"가 **실제로 존재할 때만** 트리거
            fired |= TrySetTriggerSafe(T_EndFalling);
            fired |= TrySetTriggerSafe(T_IsEndFallingAlt);

            if (!fired)
            {
                // 트리거가 없다면 인트로 체인을 수동으로 시작: Land로 바로 크로스페이드
                def?.animMap?.Play(anim, AnimKey.Land);
                // 이후 Animator의 Exit Time 전이로 Taunt → TauntOut → Idle이 이어지도록 세팅돼 있어야 함
            }

            entryLanded = true;
        }
    }

    // === Intro 진입 ===
    private void EnterIntro()
    {
        state = BossState.Intro;
        moveLocked = true;
        casting = false;
        entryLanded = false;
        entrySettled = false;

        curPhase = def ? def.phase1 : null;

        def?.animMap?.Play(anim, AnimKey.Falling, true);
        def?.animMap?.Play(anim, AnimKey.Walking, false);
    }

    // === Phase 루프 ===
    private IEnumerator PhaseLoop()
    {
        while (state == BossState.Phase1 || state == BossState.Phase2)
        {
            // 다음 공격은 반드시 Idle/Idle2에서만 시작
            yield return WaitUntilIdle();

            var move = curPhase ? curPhase.Pick(Ctx) : null;
            if (move != null)
            {
                // 공격 시작
                def?.animMap?.Play(anim, AnimKey.Walking, false);

                casting = true;
                LockMove(true);

                yield return StartCoroutine(move.Run(Ctx));

                // === 언락 순서: 캐스팅 해제 → 이동락 해제 ===
                casting = false;
                LockMove(false);

                // Idle 복귀 확인(잠금과 무관하게)
                yield return WaitUntilIdle();
            }
            else
            {
                def?.animMap?.Play(anim, AnimKey.Walking, false);
                yield return null;
            }
        }
    }

    private IEnumerator WaitUntilIdle()
    {
        while (!IsInIdle())
            yield return null;
    }

    private bool IsInIdle()
    {
        string cur = (anim != null) ? anim.GetCurClipname() : null;
        if (string.IsNullOrEmpty(cur)) return false;
        return string.Equals(cur, "Idle", StringComparison.Ordinal) ||
               string.Equals(cur, "Idle2", StringComparison.Ordinal);
    }

    private void EnsurePhaseLoop()
    {
        if (coPhase == null)
            coPhase = StartCoroutine(PhaseLoop());
    }

    private void StopPhaseLoop()
    {
        if (coPhase != null)
        {
            StopCoroutine(coPhase);
            coPhase = null;
        }
        def?.animMap?.Play(anim, AnimKey.Walking, false);
    }

    private void StartTransitionToPhase2()
    {
        if (coTransition != null) return;
        coTransition = StartCoroutine(GoTransition());
    }

    // === Phase1 → Phase2 전환 ===
    private IEnumerator GoTransition()
    {
        state = BossState.Transition;
        moveLocked = true;
        casting = false;
        def?.animMap?.Play(anim, AnimKey.Walking, false);

        if (unityAnimator) unityAnimator.SetBool(H_IsPhase2, true);

        // 전환 연출(Animator에 체인 정의되어 있다면 최소 호출로도 충분)
        def?.animMap?.Play(anim, AnimKey.Fall);
        yield return new WaitForSeconds(0.2f);
        def?.animMap?.Play(anim, AnimKey.Fallen);
        yield return new WaitForSeconds(0.2f);
        def?.animMap?.Play(anim, AnimKey.Entry2);

        // Idle2 도달까지 대기
        yield return new WaitUntil(IsInIdle);

        curPhase = def ? def.phase2 : null;
        state = BossState.Phase2;
        moveLocked = false;

        EnsurePhaseLoop();
        coTransition = null;
    }

    // === 사망 ===
    private void HandleDead()
    {
        if (this && isActiveAndEnabled)
            StartCoroutine(DieRoutine());
    }

    private IEnumerator DieRoutine()
    {
        if (state == BossState.Death) yield break;

        state = BossState.Death;
        StopPhaseLoop();
        moveLocked = true;
        casting = false;

        def?.animMap?.Play(anim, AnimKey.Walking, false);
        def?.animMap?.Play(anim, AnimKey.Death);

        yield return new WaitForSeconds(1.2f);
        // TODO: 루팅/포털 스폰 등 처리
    }

    // === 이동 잠금 토글 ===
    private void LockMove(bool on)
    {
        moveLocked = on;              // casting과 분리
        if (moveLocked) def?.animMap?.Play(anim, AnimKey.Walking, false);
        if (!rb) return;
#if UNITY_6000_0_OR_NEWER
        var v = rb.linearVelocity; v.x = moveLocked ? 0f : v.x; rb.linearVelocity = v;
#else
        var v = rb.velocity;       v.x = moveLocked ? 0f : v.x; rb.velocity = v;
#endif
    }

    // === 좌우 바라보기 (visualRoot 기준 스케일 플립) ===
    private void FaceToX(float dx)
    {
        if (Mathf.Abs(dx) < 0.0001f) return;

        bool shouldFaceRight = dx > 0f;
        bool needFlip = spriteFacesRight ? !shouldFaceRight : shouldFaceRight;

        var t = visualRoot ? visualRoot : transform;
        var s = t.localScale;
        float abs = Mathf.Abs(s.x) > 0.0001f ? Mathf.Abs(s.x) : 1f;
        s.x = needFlip ? -abs : abs;
        t.localScale = s;

        // 여러 렌더러 혼용 시 스케일 플립이 더 안전. 혼선 방지 위해 flipX는 끕니다(선택).
        if (sr) sr.flipX = false;
    }

    private void BuildAnimParamCache()
    {
        if (unityAnimator == null) return;
        _animParamSet = new HashSet<int>();
        foreach (var p in unityAnimator.parameters)
            _animParamSet.Add(p.nameHash);
    }

    private bool TrySetTriggerSafe(int hash)
    {
        if (unityAnimator == null) return false;
        if (_animParamSet == null) BuildAnimParamCache();
        if (_animParamSet.Contains(hash))
        {
            unityAnimator.SetTrigger(hash);
            return true;
        }
        return false;
    }

    // === (선택) 애니메이션 이벤트 훅 ===
    public void OnAE_Hit() { }
    public void OnAE_Spawn(string id) { }
    public void OnAE_Invuln(bool on) { invulnerable = on; }
    public void OnAE_PhaseGate() { }

    // (선택) 일부 클립에 달려 있을 수 있는 속도 초기화 이벤트 가드
    public void OnResetVelocity()
    {
        // 이동 중에는 절대 정지시키지 않음. 공격(캐스팅) 중일 때만 0 처리.
        if (!casting || rb == null) return;
#if UNITY_6000_0_OR_NEWER
        var v = rb.linearVelocity; v.x = 0f; rb.linearVelocity = v;
#else
        var v = rb.velocity;       v.x = 0f; rb.velocity = v;
#endif
    }
}
