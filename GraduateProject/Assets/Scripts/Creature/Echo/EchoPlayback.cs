using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 에코 재생 본체(업데이트):
/// - 위치/좌우 보간은 동일
/// - 메인: Animator Parameters 타임라인을 실행(1_Move, 2_Attack, 3_Damaged, 4_Death, isDeath)
/// - 보조: 녹화된 이벤트가 없으면 공격 클립명을 추정해 히트창 자동 오픈
/// - (옵션) 클립 이름 직접 Play는 비활성화(필요 시 토글)
/// </summary>
[DisallowMultipleComponent]
public class EchoPlayback : MonoBehaviour
{
    #region 변수
    [Header("Combat (optional)")]
    public AttackHitbox hitbox;
    [Range(0f, 1f)] public float damageScale = 0.5f;

    [Header("Visual")]
    public Transform visualParent;
    public Transform visualRoot;
    public Animator visualAnimator;
    [Range(0f, 1f)] public float alpha = 0.4f;

    // NEW: 클립 직접 Play를 끄고, 파라미터 기반만 사용(필요시 true)
    [Header("Animation Mode")]
    public bool playRecordedClipsDirectly = false;

    readonly List<SpriteRenderer> _renderers = new();
    EchoTape tape;
    int fi, ei, pi; // frame / action-event / anim-param indices
    float t;

    // 애니메이션 관련
    string _lastPlayedClip = null;
    float _lastPlayAt = -999f;
    const float PLAY_THROTTLE = 0.03f;

    // Trigger 디바운스(같은 트리거를 너무 자주 누르지 않도록)
    readonly Dictionary<string, float> _triggerCooldown = new();
    const float TRIGGER_EPS = 0.05f;

    static Dictionary<string, Sprite> _spriteCache;

    bool _autoAtkOpen;
    #endregion

    public void AttachVisualFrom(PlayerController player)
    {
        if (visualParent == null) visualParent = this.transform;

        if (visualRoot != null)
        {
            _renderers.Clear();
            foreach (var r in visualRoot.GetComponentsInChildren<SpriteRenderer>(true))
                _renderers.Add(r);

            if (!visualAnimator)
            {
                visualAnimator = visualRoot.GetComponentInChildren<Animator>(true);
                if (!visualAnimator) visualAnimator = GetComponent<Animator>();
            }

            ApplyAlphaToAll();
            ApplyVisualSnapshotByPath();
            return;
        }

        Transform sourceRoot = null;
        if (player != null)
        {
            var unitRoot = player.transform;
            sourceRoot = unitRoot.Find("Root");
            if (sourceRoot == null)
            {
                var all = unitRoot.GetComponentsInChildren<SpriteRenderer>(true);
                if (all != null && all.Length > 0)
                {
                    var countMap = new Dictionary<Transform, int>();
                    foreach (var rr in all)
                    {
                        var p = rr.transform.parent;
                        if (p == null) continue;
                        if (!countMap.ContainsKey(p)) countMap[p] = 0;
                        countMap[p]++;
                    }
                    Transform best = null; int bestCount = -1;
                    foreach (var kv in countMap)
                        if (kv.Value > bestCount) { bestCount = kv.Value; best = kv.Key; }
                    sourceRoot = best;
                }
            }
        }

        if (sourceRoot != null)
        {
            var clone = Instantiate(sourceRoot.gameObject, visualParent, worldPositionStays: false);
            clone.name = "[EchoGhost_VisualRoot]";
            visualRoot = clone.transform;

            foreach (var c in clone.GetComponentsInChildren<Collider2D>(true)) c.enabled = false;
            var rb = clone.GetComponentInChildren<Rigidbody2D>(true);
            if (rb) rb.simulated = false;

            visualAnimator = clone.GetComponentInChildren<Animator>(true);
            if (!visualAnimator) visualAnimator = GetComponent<Animator>();

            _renderers.Clear();
            foreach (var r in clone.GetComponentsInChildren<SpriteRenderer>(true))
                _renderers.Add(r);

            int layer = gameObject.layer;
            foreach (var tt in clone.GetComponentsInChildren<Transform>(true))
                tt.gameObject.layer = layer;

            ApplyAlphaToAll();
            ApplyVisualSnapshotByPath();
        }
        else
        {
            if (!visualAnimator) visualAnimator = GetComponent<Animator>();
            _renderers.Clear();
            foreach (var r in GetComponentsInChildren<SpriteRenderer>(true))
                _renderers.Add(r);
            ApplyAlphaToAll();
            ApplyVisualSnapshotByPath();
        }
    }

    void Update()
    {
        if (tape == null || tape.frames.Count == 0) { Destroy(gameObject); return; }

        t += Time.deltaTime;

        // 위치/좌우 보간
        while (fi + 1 < tape.frames.Count && tape.frames[fi + 1].t <= t) fi++;
        var a = tape.frames[Mathf.Clamp(fi, 0, tape.frames.Count - 1)];
        var b = tape.frames[Mathf.Clamp(fi + 1, 0, tape.frames.Count - 1)];
        float u = Mathf.Approximately(b.t, a.t) ? 0f : (t - a.t) / (b.t - a.t);
        transform.position = Vector2.Lerp(a.pos, b.pos, u);

        var s = transform.localScale;
        s.x = (a.faceRight ? Mathf.Abs(s.x) : -Mathf.Abs(s.x));
        transform.localScale = s;

        // (옵션) 예전처럼 클립 직접 Play → 기본은 OFF
        if (playRecordedClipsDirectly)
            PlayClipIfNeeded(a.clip);

        // Animator Parameter 이벤트 처리
        ProcessAnimParams();

        // 공격 Begin/End 이벤트 처리(히트박스 열고 닫기)
        bool processedEvt = false;
        while (ei < tape.events.Count && tape.events[ei].t <= t)
        {
            processedEvt = true;
            var e = tape.events[ei++];
            if (e.kind == "AtkBegin")
            {
                if (hitbox != null)
                {
                    hitbox.Source = this.gameObject;
                    if (hitbox.hitMask == 0)
                    {
                        int enemies = LayerMask.NameToLayer("Enemies");
                        if (enemies >= 0) hitbox.hitMask = 1 << enemies;
                    }
                }
                hitbox?.BeginWindow();
                _autoAtkOpen = false;
            }
            else if (e.kind == "AtkEnd")
            {
                hitbox?.EndWindow();
                _autoAtkOpen = false;
            }
        }

        // (백업) 이벤트 전혀 없으면 공격스러운 클립명 동안 자동 히트창
        if (!processedEvt && hitbox != null && !string.IsNullOrEmpty(a.clip))
        {
            bool isAttackClip = IsAttackClipName(a.clip);
            if (isAttackClip && !_autoAtkOpen)
            {
                hitbox.Source = this.gameObject;
                hitbox.BeginWindow();
                _autoAtkOpen = true;
            }
            else if (!isAttackClip && _autoAtkOpen)
            {
                hitbox.EndWindow();
                _autoAtkOpen = false;
            }
        }
    }

    public void Load(EchoTape t_)
    {
        tape = t_;
        fi = 0; ei = 0; pi = 0; t = 0f;
        _lastPlayedClip = null;
        _autoAtkOpen = false;
        _triggerCooldown.Clear();

        if (hitbox)
        {
            hitbox.baseDamage *= damageScale;
            if (hitbox.hitMask == 0)
            {
                int enemies = LayerMask.NameToLayer("Enemies");
                if (enemies >= 0) hitbox.hitMask = 1 << enemies;
            }
        }
    }

    public void SetAlpha(float a) { alpha = Mathf.Clamp01(a); ApplyAlphaToAll(); }

    void ApplyAlphaToAll()
    {
        if (_renderers.Count == 0) return;
        for (int i = 0; i < _renderers.Count; i++)
        {
            if (_renderers[i] == null) continue;
            var c = _renderers[i].color; c.a = alpha; _renderers[i].color = c;
        }
    }

    // === Animator Parameter 타임라인 실행 ===
    void ProcessAnimParams()
    {
        if (visualAnimator == null) return;
        if (tape.animParams == null || tape.animParams.Count == 0) return;

        while (pi < tape.animParams.Count && tape.animParams[pi].t <= t)
        {
            var p = tape.animParams[pi++];
            if (p.type == "bool")
            {
                if (p.name == "isDeath")
                    visualAnimator.SetBool("isDeath", p.value != 0);
                else if (p.name == "1_Move")
                    visualAnimator.SetBool("1_Move", p.value != 0);
                else
                    visualAnimator.SetBool(p.name, p.value != 0);
            }
            else if (p.type == "trig")
            {
                if (CanFireTrigger(p.name))
                    visualAnimator.SetTrigger(p.name);
            }
        }
    }

    bool CanFireTrigger(string trig)
    {
        float now = Time.time;
        if (_triggerCooldown.TryGetValue(trig, out var last) && (now - last) < TRIGGER_EPS)
            return false;
        _triggerCooldown[trig] = now;
        return true;
    }

    // (옵션) 레거시: 녹화된 클립 이름을 직접 재생
    void PlayClipIfNeeded(string recordedClip)
    {
        if (!visualAnimator) return;
        if (string.IsNullOrEmpty(recordedClip)) return;
        if (Time.time - _lastPlayAt < PLAY_THROTTLE && recordedClip == _lastPlayedClip) return;

        visualAnimator.Play(recordedClip, 0, 0f);
        _lastPlayedClip = recordedClip;
        _lastPlayAt = Time.time;
    }

    static bool IsAttackClipName(string name)
    {
        var n = name.ToUpperInvariant();
        return n.Contains("ATTACK") || n.Contains("ATK") || n.Contains("SLASH") || n.Contains("HIT") || n.Contains("2_ATTACK");
    }

    void ApplyVisualSnapshotByPath()
    {
        if (visualRoot == null && transform != null)
        {
            var maybe = transform.Find("Root");
            if (maybe) visualRoot = maybe;
        }

        if (visualRoot == null || tape == null || tape.visualParts == null || tape.visualParts.Count == 0)
            return;

        if (_spriteCache == null) _spriteCache = new Dictionary<string, Sprite>(128);

        foreach (var vp in tape.visualParts)
        {
            if (string.IsNullOrEmpty(vp.path)) continue;

            var target = visualRoot.Find(vp.path);
            if (target == null) continue;

            var sr = target.GetComponent<SpriteRenderer>();
            if (sr == null) continue;

            if (!string.IsNullOrEmpty(vp.sprite))
            {
                var sp = FindSpriteByNameCached(vp.sprite);
                if (sp != null) sr.sprite = sp;
            }

            sr.enabled = vp.enabled;
            sr.transform.localPosition += (Vector3)vp.localPosOffset;
            var ls = sr.transform.localScale;
            sr.transform.localScale = new Vector3(ls.x * (Mathf.Approximately(vp.localScaleMul.x, 0f) ? 1f : vp.localScaleMul.x),
                                                  ls.y * (Mathf.Approximately(vp.localScaleMul.y, 0f) ? 1f : vp.localScaleMul.y),
                                                  ls.z);
            sr.sortingOrder += vp.sortingOffset;

            if (vp.changeMaskInteraction)
                sr.maskInteraction = (SpriteMaskInteraction)vp.maskInteraction;

            var mask = sr.GetComponent<SpriteMask>();
            if (mask) mask.enabled = vp.enablePartSpriteMask;
        }
    }

    static Sprite FindSpriteByNameCached(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (_spriteCache.TryGetValue(name, out var s) && s != null) return s;

        var all = Resources.FindObjectsOfTypeAll<Sprite>();
        for (int i = 0; i < all.Length; i++)
        {
            var sp = all[i];
            if (sp != null && string.Equals(sp.name, name, StringComparison.OrdinalIgnoreCase))
            {
                _spriteCache[name] = sp;
                return sp;
            }
        }
        _spriteCache[name] = null;
        return null;
    }
}
