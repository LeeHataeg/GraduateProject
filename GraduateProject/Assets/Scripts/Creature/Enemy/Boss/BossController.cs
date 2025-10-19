using UnityEngine;
using System.Collections;
using System;
using BossState = Define.BossState;
using AnimKey = Define.AnimKey;
using System.Collections.Generic;
using static AnimMapSO;
using Unity.VisualScripting.Antlr3.Runtime.Misc;

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
    [SerializeField] private float moveSpeed = 6.0f;
    [SerializeField] private float accel = 20.0f;
    [SerializeField] private float stopDist = 2.5f;

    [Header("Visual Root / RB Guard (추가)")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private bool forceUnfreezeX = true;
    [SerializeField] private bool forceDynamicRB = true;

    // internals
    private HealthController hp;
    private Rigidbody2D rb;
    private IAnimationController anim;   // AnimatorAdaptor
    private Animator unityAnimator;      // 파라미터 직접 접근
    private BossState state;
    private BossPhaseSO curPhase;
    private Coroutine coPhase;
    private Coroutine coTransition;
    private HashSet<int> _animParamSet;

    private bool moveLocked = true;  // Intro 동안 잠금
    public bool IsMoveLocked => moveLocked;
    private bool invulnerable;
    private bool casting;            // 공격 시전 중

    public bool IsInvulnerable => invulnerable;

    // Intro 플래그
    private bool entryLanded;
    private bool entrySettled;

    // Debug
    public float deltaX;

    // Animator 파라미터 해시
    private static readonly int H_IsPhase2 = Animator.StringToHash("IsPhase2");
    private static readonly int H_IsDead = Animator.StringToHash("IsDead");
    private static readonly int H_IsStun = Animator.StringToHash("IsStun");
    private static readonly int H_SpeedBlend = Animator.StringToHash("SpeedBlend");
    private static readonly int H_MoveX = Animator.StringToHash("MoveX");

    private static readonly int T_EndFalling = Animator.StringToHash("EndFalling");
    private static readonly int T_IsEndFallingAlt = Animator.StringToHash("IsEndFalling");

    [SerializeField] private bool introFallingSet = false;

    public event System.Action GroundTouched;

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

        if (unityAnimator) unityAnimator.applyRootMotion = false;

        if (!def) Debug.LogError("[BossController] Definition SO 미할당!");
        def?.animMap?.Build();
        BuildAnimParamCache();
        ConfigureRB();
    }

    private void OnEnable()
    {
        Debug.Log("hp.CurrentHp : " + hp.CurrentHp);
    }
    private void ConfigureRB()
    {
        if (!rb) return;

        if (forceDynamicRB) rb.bodyType = RigidbodyType2D.Dynamic;
        rb.simulated = true;
        rb.freezeRotation = true;

        if (forceUnfreezeX)
            rb.constraints &= ~RigidbodyConstraints2D.FreezePositionX;
    }

    private void Start()
    {
        if (autoFindPlayer && UnitRoot == null)
            UnitRoot = GameObject.FindGameObjectWithTag("Player");

        if (hp != null) hp.OnDead += HandleDead;

        EnterIntro();

        // 빠른 진단: 짧은 상태명으로 HasState 체크 (Base Layer 접두 불필요)
        // 빠른 진단: 짧은 상태명으로 HasState 체크 (Base Layer 접두 불필요)
        if (unityAnimator)
        {
            Debug.Log($"Has Atk1? {unityAnimator.HasState(0, Animator.StringToHash("Atk1"))}");
            Debug.Log($"Has Atk2? {unityAnimator.HasState(0, Animator.StringToHash("Atk2"))}");
            Debug.Log($"Has Atk3? {unityAnimator.HasState(0, Animator.StringToHash("Atk3"))}");
        }

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
        // 파라미터 동기화
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

        // 상태 머신
        if (state == BossState.Intro)
        {
            // ★ 더 이상 Falling을 매 프레임 강제하지 않는다.
            //   (EnterIntro에서 한 번만 재생했음)

            if (!introFallingSet) { def?.animMap?.Play(anim, AnimKey.Falling); introFallingSet = true; }

            if (entryLanded && IsInIdle())
            {
                entrySettled = true;
                moveLocked = false;
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
            // HP 게이트
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

            // 콤보/공격 중엔 루프/로코모션 금지
            if (!moveLocked)
                EnsurePhaseLoop();

            if (!moveLocked)
                HandleLocomotion();
        }
    }

    public void SetMoveLocked(bool value)
    {
        moveLocked = value;
        if (moveLocked && rb)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector2.zero;
#else
            rb.velocity = Vector2.zero;
#endif
            rb.angularVelocity = 0f;
        }
    }

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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null) return;

        // === 기존 Intro 처리 ===
        if (state == BossState.Intro && moveLocked && collision.gameObject.CompareTag("Ground"))
        {
            bool fired = false;
            fired |= TrySetTriggerSafe(T_EndFalling);
            fired |= TrySetTriggerSafe(T_IsEndFallingAlt);

            if (!fired)
            {
                // 트리거가 없으면 Land 상태로 직접 넘어가서 체인 촉발
                def?.animMap?.Play(anim, AnimKey.Land);
            }
            entryLanded = true;

            // ★ Ground 이벤트도 알림 (Intro에서도 통일)
            GroundTouched?.Invoke();
            return;
        }

        // === 일반 Ground 접촉 알림 ===
        if (collision.gameObject.CompareTag("Ground"))
        {
            GroundTouched?.Invoke();
        }
    }

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

    private IEnumerator PhaseLoop()
    {
        while (state == BossState.Phase1 || state == BossState.Phase2)
        {
            yield return WaitUntilIdle();

            var move = curPhase ? curPhase.Pick(Ctx) : null;
            if (move != null)
            {
                def?.animMap?.Play(anim, AnimKey.Walking, false);

                casting = true;
                LockMove(true);

                yield return StartCoroutine(move.Run(Ctx));

                casting = false;
                LockMove(false);

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

    private IEnumerator GoTransition()
    {
        state = BossState.Transition;
        moveLocked = true;
        casting = false;
        def?.animMap?.Play(anim, AnimKey.Walking, false);

        if (unityAnimator) unityAnimator.SetBool(H_IsPhase2, true);

        def?.animMap?.Play(anim, AnimKey.Fall);
        yield return new WaitForSeconds(0.2f);
        def?.animMap?.Play(anim, AnimKey.Fallen);
        yield return new WaitForSeconds(0.2f);
        def?.animMap?.Play(anim, AnimKey.Entry2);

        yield return new WaitUntil(IsInIdle);

        curPhase = def ? def.phase2 : null;
        state = BossState.Phase2;
        moveLocked = false;

        EnsurePhaseLoop();
        coTransition = null;
    }

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
        // TODO: 루팅/포털 스폰 등
    }

    private void LockMove(bool on)
    {
        moveLocked = on;
        if (moveLocked) def?.animMap?.Play(anim, AnimKey.Walking, false);
        if (!rb) return;
#if UNITY_6000_0_OR_NEWER
        var v = rb.linearVelocity; v.x = moveLocked ? 0f : v.x; rb.linearVelocity = v;
#else
        var v = rb.velocity;       v.x = moveLocked ? 0f : v.x; rb.velocity = v;
#endif
    }

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
        if (_animParamSet == null)
        {
            _animParamSet = new HashSet<int>();
            foreach (var p in unityAnimator.parameters) _animParamSet.Add(p.nameHash);
        }
        if (_animParamSet.Contains(hash)) { unityAnimator.SetTrigger(hash); return true; }
        return false;
    }

    // Animation Event hooks
    public void OnAE_Hit() { }
    public void OnAE_Spawn(string id) { }
    public void OnAE_Invuln(bool on) { invulnerable = on; }
    public void OnAE_PhaseGate() { }

    public void OnResetVelocity()
    {
        if (!casting || rb == null) return;
#if UNITY_6000_0_OR_NEWER
        var v = rb.linearVelocity; v.x = 0f; rb.linearVelocity = v;
#else
        var v = rb.velocity;       v.x = 0f; rb.velocity = v;
#endif
    }
}
