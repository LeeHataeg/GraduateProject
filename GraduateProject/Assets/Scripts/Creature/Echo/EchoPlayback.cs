using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 에코 재생 본체:
/// - 위치/좌우 반전/공격 윈도우 재생
/// - 프리팹에 비주얼이 있으면 그대로 사용, 없으면 플레이어 "Root"를 복제
/// - 녹화 프레임의 clip 이름을 Animator에 그대로 재생
/// - 테이프의 '경로 기반' 외형 스냅샷 적용(사망 당시 외형 재현)
/// - (중요) 녹화된 이벤트가 없어도, "공격으로 보이는" 클립명 동안 자동 히트창 오픈
/// </summary>
[DisallowMultipleComponent]
public class EchoPlayback : MonoBehaviour
{
    #region 변수
    [Header("Combat (optional)")]
    public AttackHitbox hitbox;           // Ghost가 Enemy를 때리는 오브젝트
    [Range(0f, 1f)] public float damageScale = 0.5f;

    [Header("Visual")]
    public Transform visualParent;        // 최상위 부모 오브젝트
    public Transform visualRoot;          // 각 신체 파츠를 담고 있는 오브젝트
    public Animator visualAnimator;
    [Range(0f, 1f)] public float alpha = 0.4f;


    readonly List<SpriteRenderer> _renderers = new();
    EchoTape tape;
    int fi, ei;
    float t;

    // 애니메이션 관련 변수들...
    string _lastPlayedClip = null;
    float _lastPlayAt = -999f;
    const float PLAY_THROTTLE = 0.03f;

    static Dictionary<string, Sprite> _spriteCache; //캐싱

    bool _autoAtkOpen;
    #endregion

    public void AttachVisualFrom(PlayerController player)
    {
        if (visualParent == null) visualParent = this.transform;

        // 프리팹에 이미 비주얼이 있음? 그거 쓰셈.
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

        // 프리팹에 비주얼이 없음? 플레이어 UnitRoot/Root를 복제하셈
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
            // 비주얼을 못 준비했어도 Animator가 루트에 붙어 있다면 그걸 사용
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

        // 위치/좌우 설정
        while (fi + 1 < tape.frames.Count && tape.frames[fi + 1].t <= t) fi++;
        var a = tape.frames[Mathf.Clamp(fi, 0, tape.frames.Count - 1)];
        var b = tape.frames[Mathf.Clamp(fi + 1, 0, tape.frames.Count - 1)];
        float u = Mathf.Approximately(b.t, a.t) ? 0f : (t - a.t) / (b.t - a.t);

        transform.position = Vector2.Lerp(a.pos, b.pos, u);
         
        // 좌우 반전
        var s = transform.localScale;
        s.x = (a.faceRight ? Mathf.Abs(s.x) : -Mathf.Abs(s.x));
        transform.localScale = s;

        // 애니메이션 클립 재생 ㄱㄱ
        PlayClipIfNeeded(a.clip);

        // 녹화 기반으로 공격 ㄱㄱ
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

        // 이벤트가 하나도 없음?
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
        fi = 0; ei = 0; t = 0f;
        _lastPlayedClip = null;
        _autoAtkOpen = false;

        if (hitbox)
        {
            hitbox.baseDamage *= damageScale;

            // HitMask가 비어 있으면 'Enemies'로 적용
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

   
    void PlayClipIfNeeded(string recordedClip)
    {
        if (!visualAnimator)
        {
            // 혹시 비워져 있으면 마지막으로 한 번 더 자동 탐색(루트/자식)
            visualAnimator = GetComponent<Animator>();
            if (!visualAnimator && visualRoot) visualAnimator = visualRoot.GetComponentInChildren<Animator>(true);
            if (!visualAnimator) return;
        }
        if (string.IsNullOrEmpty(recordedClip)) return;
        if (Time.time - _lastPlayAt < PLAY_THROTTLE && recordedClip == _lastPlayedClip) return;

        visualAnimator.Play(recordedClip, 0, 0f);
        _lastPlayedClip = recordedClip;
        _lastPlayAt = Time.time;
    }

    // 공격처럼 보이는 이름 패턴(네 프로젝트 클립명에 맞춰 확장)
    static bool IsAttackClipName(string name)
    {
        // 대소문자 무시, 다양한 관용 패턴 지원
        var n = name.ToUpperInvariant();
        return n.Contains("ATTACK") || n.Contains("ATK") || n.Contains("SLASH") || n.Contains("HIT") || n.Contains("2_ATTACK");
    }

    // ─────────────────────────────────────────────────────────
    //           경로 기반 스냅샷 적용(사망 당시 외형 재현)
    // ─────────────────────────────────────────────────────────
    void ApplyVisualSnapshotByPath()
    {
        if (visualRoot == null && transform != null)
        {
            // 프리팹 구조에 따라 Root가 바로 자식일 수도, 아닐 수도 있음 → 최선 탐색
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

            // 스프라이트 적용
            if (!string.IsNullOrEmpty(vp.sprite))
            {
                var sp = FindSpriteByNameCached(vp.sprite);
                if (sp != null) sr.sprite = sp;
            }

            // 트랜스폼/소팅/활성
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
