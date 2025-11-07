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
    public Transform attackOrigin;
    public GameObject UnitRoot;

    [Header("Optional")]
    public bool autoFindPlayer = true;

    [Header("Facing")]
    public bool spriteFacesRight = true;
    [SerializeField] private SpriteRenderer sr;

    [Header("Locomotion")]
    [SerializeField] private float moveSpeed = 6.0f;
    [SerializeField] private float accel = 20.0f;
    [SerializeField] private float stopDist = 2.5f;

    [Header("Visual Root / RB Guard")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private bool forceUnfreezeX = true;
    [SerializeField] private bool forceDynamicRB = true;

    // internals
    private HealthController hp;
    private Rigidbody2D rb;
    private IAnimationController anim;
    private Animator unityAnimator;
    private BossState state;
    private BossPhaseSO curPhase;
    private Coroutine coPhase;
    private Coroutine coTransition;
    private HashSet<int> _animParamSet;

    private bool moveLocked = true;
    public bool IsMoveLocked => moveLocked;
    private bool invulnerable;
    private bool casting;

    public bool IsInvulnerable => invulnerable;


    public event System.Action GroundTouched;

    private static readonly int H_IsPhase2 = Animator.StringToHash("IsPhase2");
    private static readonly int H_IsDead = Animator.StringToHash("IsDead");
    private static readonly int H_IsStun = Animator.StringToHash("IsStun");
    private static readonly int H_SpeedBlend = Animator.StringToHash("SpeedBlend");
    private static readonly int H_MoveX = Animator.StringToHash("MoveX");

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

        if (unityAnimator) unityAnimator.applyRootMotion = false;
        if (!def) Debug.LogError("[BossController] Definition SO 미할당!");
        def?.animMap?.Build();
        BuildAnimParamCache();
        ConfigureRB();
    }

    private void ConfigureRB()
    {
        if (!rb) return;
        if (forceDynamicRB) rb.bodyType = RigidbodyType2D.Dynamic;
        rb.simulated = true;
        rb.freezeRotation = true;
        if (forceUnfreezeX) rb.constraints &= ~RigidbodyConstraints2D.FreezePositionX;
    }

    private void Start()
    {
        if (autoFindPlayer && UnitRoot == null)
            UnitRoot = GameObject.FindGameObjectWithTag("Player");

        if (hp != null) hp.OnDead += HandleDead;

        // === Intro 형식 분기 ===
        if (def != null && def.introMode == BossDefinitionSO.IntroMode.PlayEntryOnce)
            EnterIntro_EntryOnce();
        else
            EnterIntro_FallFromSky();
    }

    private void OnDestroy()
    {
        if (hp != null) hp.OnDead -= HandleDead;
    }

    private void Update()
    {
        // Animator 파라미터 동기화
        if (unityAnimator)
        {
            bool isPhase2 = (state == BossState.Phase2);
            unityAnimator.SetBool(H_IsPhase2, isPhase2);
            unityAnimator.SetBool(H_IsDead, state == BossState.Death);

            float dist = 0f; float moveX = 0f;
            if (UnitRoot)
            {
                var p = UnitRoot.transform.position;
                dist = Vector2.Distance(p, transform.position);
                moveX = Mathf.Clamp(p.x - transform.position.x, -1f, 1f);
            }
            unityAnimator.SetFloat(H_SpeedBlend, dist);
            unityAnimator.SetFloat(H_MoveX, moveX);
        }
    }

    private void FixedUpdate()
    {
        if (state == BossState.Intro || state == BossState.Transition || state == BossState.Death)
            return;

        if (state == BossState.Phase1 || state == BossState.Phase2)
        {
            // Phase2 게이트: singlePhase거나 phase2==null이면 스킵
            if (state == BossState.Phase1 && def && !def.singlePhase && def.phase2 != null && hp != null)
            {
                float gate = def.phase1 != null ? def.phase1.toNextPhaseHpRate : 0f;
                float maxHp = hp.Stats != null ? hp.Stats.MaxHp : Mathf.Max(1, hp.MaxHp);
                if (gate > 0f && hp.CurrentHp <= maxHp * gate)
                {
                    StopPhaseLoop();
                    StartTransitionToPhase2();
                    return;
                }
            }

            if (!moveLocked) EnsurePhaseLoop();
            if (!moveLocked) HandleLocomotion();
        }
    }

    private void EnterIntro_FallFromSky()
    {
        state = BossState.Intro;
        moveLocked = true;
        casting = false;
        curPhase = def ? def.phase1 : null;

        def?.animMap?.Play(anim, AnimKey.Falling, true);
        def?.animMap?.Play(anim, def ? def.walkingBoolKey : AnimKey.Walking, false);
    }

    private void EnterIntro_EntryOnce()
    {
        state = BossState.Intro;
        moveLocked = true; casting = false;
        curPhase = def ? def.phase1 : null;

        def?.animMap?.Play(anim, def ? def.walkingBoolKey : AnimKey.Walking, false);
        def?.animMap?.Play(anim, def ? def.entryKey : AnimKey.Entry);

        StartCoroutine(Co_WaitEntryThenStartPhase());
    }

    private IEnumerator Co_WaitEntryThenStartPhase()
    {
        // Entry → Animator에서 Idle로 넘어가길 기다림
        float t = 3f;
        while (t > 0f && !IsInIdle()) { t -= Time.deltaTime; yield return null; }

        state = BossState.Phase1;
        moveLocked = false;
        EnsurePhaseLoop();
    }

    private void StartTransitionToPhase2()
    {
        if (coTransition != null) return;
        coTransition = StartCoroutine(GoTransition());
    }

    private IEnumerator GoTransition()
    {
        state = BossState.Transition;
        moveLocked = true; casting = false;
        def?.animMap?.Play(anim, def ? def.walkingBoolKey : AnimKey.Walking, false);

        if (unityAnimator) unityAnimator.SetBool(H_IsPhase2, true);
        def?.animMap?.Play(anim, AnimKey.Fall); yield return new WaitForSeconds(0.2f);
        def?.animMap?.Play(anim, AnimKey.Fallen); yield return new WaitForSeconds(0.2f);
        def?.animMap?.Play(anim, AnimKey.Entry2);

        yield return new WaitUntil(IsInIdle);

        curPhase = def ? def.phase2 : null;
        state = BossState.Phase2;
        moveLocked = false;

        EnsurePhaseLoop();
        coTransition = null;
    }

    private void HandleLocomotion()
    {
        if (moveLocked || casting) { HaltHorizontal(); return; }
        if (!rb || !UnitRoot) { HaltHorizontal(); return; }

        float dx = UnitRoot.transform.position.x - transform.position.x;
        float adx = Mathf.Abs(dx);
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

        if (state == BossState.Intro && def != null && def.introMode == BossDefinitionSO.IntroMode.FallFromSky
            && moveLocked && collision.gameObject.CompareTag("Ground"))
        {
            bool fired = false;
            fired |= TrySetTriggerSafe(T_EndFalling);
            fired |= TrySetTriggerSafe(T_IsEndFallingAlt);
            if (!fired) def?.animMap?.Play(anim, AnimKey.Land);
            GroundTouched?.Invoke();

            // Land → Idle로 넘어가면 페이즈 시작
            StartCoroutine(Co_WaitIntroLandToIdle());
            return;
        }

        if (collision.gameObject.CompareTag("Ground"))
            GroundTouched?.Invoke();
    }

    private IEnumerator Co_WaitIntroLandToIdle()
    {
        float t = 3f;
        while (t > 0f && !IsInIdle()) { t -= Time.deltaTime; yield return null; }
        moveLocked = false;
        state = BossState.Phase1;
        EnsurePhaseLoop();
    }

    private void EnsurePhaseLoop()
    {
        if (coPhase == null) coPhase = StartCoroutine(PhaseLoop());
    }

    private void StopPhaseLoop()
    {
        if (coPhase != null) { StopCoroutine(coPhase); coPhase = null; }
        def?.animMap?.Play(anim, def ? def.walkingBoolKey : AnimKey.Walking, false);
    }

    private IEnumerator PhaseLoop()
    {
        while (state == BossState.Phase1 || state == BossState.Phase2)
        {
            // Idle 브레이크
            var wait = curPhase ? curPhase.GetIdleBreak() : 0.3f;
            yield return new WaitForSeconds(wait);

            // Idle 상태 보장까지 대기
            yield return WaitUntilIdle();

            var move = curPhase ? curPhase.Pick(Ctx) : null;
            if (move != null)
            {
                def?.animMap?.Play(anim, def ? def.walkingBoolKey : AnimKey.Walking, false);
                casting = true; LockMove(move.lockMovement);

                yield return StartCoroutine(move.Run(Ctx));

                casting = false; LockMove(false);

                yield return WaitUntilIdle();
            }
            else
            {
                def?.animMap?.Play(anim, def ? def.walkingBoolKey : AnimKey.Walking, false);
                yield return null;
            }
        }
    }

    private IEnumerator WaitUntilIdle()
    {
        while (!IsInIdle()) yield return null;
    }

    private bool IsInIdle()
    {
        string cur = (anim != null) ? anim.GetCurClipname() : null;
        if (string.IsNullOrEmpty(cur)) return false;

        // 기본 Idle 이름 또는 Definition에서 지정한 idleKey 상태명(AnimMapSO의 param)을 허용
        if (string.Equals(cur, "Idle", StringComparison.Ordinal) ||
            string.Equals(cur, "Idle2", StringComparison.Ordinal)) return true;

        if (def != null && def.animMap != null)
        {
            var field = typeof(AnimMapSO).GetField("_map", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field != null)
            {
                var map = field.GetValue(def.animMap) as System.Collections.IDictionary;
                if (map != null && map.Contains(def.idleKey))
                {
                    var pair = map[def.idleKey];
                    var pParam = pair.GetType().GetField("param");
                    if (pParam != null)
                    {
                        string stateName = pParam.GetValue(pair) as string;
                        if (!string.IsNullOrEmpty(stateName) &&
                            string.Equals(cur, stateName, StringComparison.Ordinal)) return true;
                    }
                }
            }
        }
        return false;
    }

    private void LockMove(bool on)
    {
        moveLocked = on;
        if (moveLocked) def?.animMap?.Play(anim, def ? def.walkingBoolKey : AnimKey.Walking, false);
        if (!rb) return;
#if UNITY_6000_0_OR_NEWER
        var v = rb.linearVelocity; v.x = moveLocked ? 0f : v.x; rb.linearVelocity = v;
#else
        var v = rb.velocity;       v.x = moveLocked ? 0f : v.x; rb.velocity = v;
#endif
    }

    // ★ ADDED: public wrappers so external Moves can control locomotion lock
    public void SetMoveLocked(bool on) => LockMove(on);
    public bool GetMoveLocked() => moveLocked;

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
        foreach (var p in unityAnimator.parameters) _animParamSet.Add(p.nameHash);
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

    private void HandleDead()
    {
        if (this && isActiveAndEnabled) StartCoroutine(DieRoutine());
    }

    private IEnumerator DieRoutine()
    {
        if (state == BossState.Death) yield break;

        state = BossState.Death;
        StopPhaseLoop();
        moveLocked = true; casting = false;

        def?.animMap?.Play(anim, def ? def.walkingBoolKey : AnimKey.Walking, false);
        def?.animMap?.Play(anim, def ? def.deathKey : AnimKey.Death);

        yield return new WaitForSeconds(def ? def.fadeDelay : 1.2f);
        // TODO: 전리품/포털 등 처리 지점
    }

    // Animation Event hooks
    public void OnAE_Hit() { }
    public void OnAE_Spawn(string id) { }
    public void OnAE_Invuln(bool on) { invulnerable = on; }
    public void OnAE_PhaseGate() { }
}
