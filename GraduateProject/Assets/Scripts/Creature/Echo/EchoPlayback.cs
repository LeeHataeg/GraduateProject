using System;
using System.Collections.Generic;
using UnityEngine;

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

    public bool playRecordedClipsDirectly = false;

    readonly List<SpriteRenderer> _renderers = new();
    EchoTape tape;
    int frame, actionEvent, animParameters; // 위치, 
    float t;

    // 애니메이션의 과도한 반복을 제한할거임
    readonly Dictionary<string, float> triggerCooldown = new();
    const float TRIGGER_EPS = 0.05f;

    static Dictionary<string, Sprite> spriteCache;

    bool _autoAtkOpen;

    [Header("Ranged Echo Attack")]
    [Tooltip("유령이 총을 쏠 위치 (없으면 자기 transform.position 사용)")]
    public Transform rangedFirePoint;

    [Tooltip("장비 SO에서 못 찾았을 때 쓸 기본 탄환 프리팹")]
    public GameObject fallbackBulletPrefab;

    [Tooltip("기본 총알 속도 (장비 SO에 없을 때)")]
    public float defaultBulletSpeed = 10f;

    [Tooltip("기본 총알 수명 (장비 SO에 없을 때)")]
    public float defaultBulletLifeTime = 3f;

    [Tooltip("총알이 맞출 레이어(비어 있으면 자동으로 Enemies 레이어 찾음)")]
    public LayerMask rangedHitMask;

    [Range(0f, 5f)]
    [Tooltip("EchoGhost 원거리 공격 데미지 배율")]
    public float rangedDamageScale = 0.5f;

    // 런타임용
    private EquipmentItemData rangedWeaponData;
    private GameObject rangedBulletPrefab;
    private float rangedBulletSpeed;
    private float rangedBulletLife;

    bool HasRangedWeapon => rangedWeaponData != null;
    bool CanShootRanged => HasRangedWeapon || fallbackBulletPrefab != null;
    #endregion

    // 비주얼
    public void AttachVisualFrom(PlayerController player)
    {
        if (visualParent == null) visualParent = this.transform;

        if (visualRoot != null)
        {
            // Echo에 대하여 모든 하위 SR을 캐싱 ㄱㄱ
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

    //플레이어 ㅇㅇ
    void Update()
    {
        // 재생 데이터 없다면 유령 제거
        if (tape == null || tape.frames.Count == 0) {
            Destroy(gameObject); return; 
        }

        t += Time.deltaTime;

        // 위치 설정

        // 현재 시간 t를 지날 때 까지 ㅇ오른쪽 프레임 인덱스로 전진
        while (frame + 1 < tape.frames.Count && tape.frames[frame + 1].t <= t) 
            frame++;

        // data의 양 끝 프레임 확보
        var a = tape.frames[Mathf.Clamp(frame, 0, tape.frames.Count - 1)];
        var b = tape.frames[Mathf.Clamp(frame + 1, 0, tape.frames.Count - 1)];

        // 두 구간에서 보간하여 계산
        float u = Mathf.Approximately(b.t, a.t) ? 0f : (t - a.t) / (b.t - a.t);
        transform.position = Vector2.Lerp(a.pos, b.pos, u);

        var s = transform.localScale;
        s.x = (a.faceRight ? Mathf.Abs(s.x) : -Mathf.Abs(s.x)); // flip
        transform.localScale = s;

        // Animator의 Parameters를 통해 이벤트 처리 ㄱㄱ
        ProcessAnimParams();

        // Echo 공격 시작 종료 관리
        bool processedEvt = false;
        while (actionEvent < tape.events.Count && tape.events[actionEvent].t <= t)
        {
            processedEvt = true;
            var e = tape.events[actionEvent++];

            if (e.kind == "AtkBegin")
            {
                bool rangedEvt = string.Equals(e.id, "Ranged", StringComparison.OrdinalIgnoreCase);

                if (rangedEvt && CanShootRanged)
                {
                    // ★ 원거리 공격: 기록된 각도로 총알 발사
                    FireRangedShot(e.value);
                    _autoAtkOpen = false;
                }
                else
                {
                    // ★ 근접 공격 (기존 로직 유지)
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
            }
            else if (e.kind == "AtkEnd")
            {
                bool rangedEvt = string.Equals(e.id, "Ranged", StringComparison.OrdinalIgnoreCase);

                if (!rangedEvt)
                {
                    // 근접 공격만 창 닫기
                    hitbox?.EndWindow();
                }
                _autoAtkOpen = false;
            }
        }

        if (!processedEvt && hitbox != null && !string.IsNullOrEmpty(a.clip) && !HasRangedWeapon)
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

    // 로드
    public void Load(EchoTape t_)
    {
        tape = t_;
        frame = 0; actionEvent = 0; animParameters = 0; t = 0f;
        _autoAtkOpen = false;
        triggerCooldown.Clear();

        if (hitbox)
        {
            hitbox.baseDamage *= damageScale;
            if (hitbox.hitMask == 0)
            {
                int enemies = LayerMask.NameToLayer("Enemies");
                if (enemies >= 0) hitbox.hitMask = 1 << enemies;
            }
        }

        SetupRangedWeaponFromTape();
    }

    private void SetupRangedWeaponFromTape()
    {
        rangedWeaponData = null;
        rangedBulletPrefab = null;

        if (tape == null || tape.equipped == null)
            return;

        // EchoTape.equipped 안에서 Weapon 슬롯 찾기
        foreach (var eq in tape.equipped)
        {
            if (string.Equals(eq.slot, "Weapon", StringComparison.OrdinalIgnoreCase))
            {
                // 이름으로 EquipmentItemData 찾아오기
                var all = Resources.FindObjectsOfTypeAll<EquipmentItemData>();
                foreach (var data in all)
                {
                    if (data != null && string.Equals(data.name, eq.itemId, StringComparison.OrdinalIgnoreCase))
                    {
                        rangedWeaponData = data;
                        break;
                    }
                }
                break;
            }
        }

        // 실제로 원거리 무기인지 확인
        if (rangedWeaponData != null && rangedWeaponData.IsRanged)
        {
            rangedBulletPrefab = rangedWeaponData.Bullet;
            rangedBulletSpeed = (rangedWeaponData.BulletSpeed > 0f) ? rangedWeaponData.BulletSpeed : defaultBulletSpeed;
            rangedBulletLife = (rangedWeaponData.BulletLifeTime > 0f) ? rangedWeaponData.BulletLifeTime : defaultBulletLifeTime;
        }
        else
        {
            rangedWeaponData = null;
            rangedBulletPrefab = null;
            rangedBulletSpeed = defaultBulletSpeed;
            rangedBulletLife = defaultBulletLifeTime;
        }

        // 총알 히트 마스크 기본값 세팅
        if (rangedHitMask == 0)
        {
            int enemies = LayerMask.NameToLayer("Enemies");
            if (enemies >= 0) rangedHitMask = 1 << enemies;
            else rangedHitMask = ~0;   // 최후의 수단: 전부
        }
    }

    private void FireRangedShot(float angleDeg)
    {
        if (!CanShootRanged)
            return;

        GameObject prefab = rangedBulletPrefab != null ? rangedBulletPrefab : fallbackBulletPrefab;
        if (prefab == null)
            return;

        Vector3 pos = (rangedFirePoint != null) ? rangedFirePoint.position : transform.position;

        var go = Instantiate(prefab, pos, Quaternion.identity);
        var b = go.GetComponent<Bullet>();
        if (b == null) b = go.AddComponent<Bullet>();

        float rad = angleDeg * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

        if (dir.sqrMagnitude < 0.0001f)
            dir = (transform.localScale.x >= 0f) ? Vector2.right : Vector2.left;

        float dmg = (hitbox != null)
            ? hitbox.baseDamage * rangedDamageScale
            : 10f;

        b.Init(dir.normalized, dmg, this.gameObject, rangedHitMask, rangedBulletSpeed, rangedBulletLife);
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

    void ProcessAnimParams()
    {
        if (visualAnimator == null) return;
        if (tape.animParams == null || tape.animParams.Count == 0) return;

        while (animParameters < tape.animParams.Count && tape.animParams[animParameters].t <= t)
        {
            var p = tape.animParams[animParameters++];
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
        if (triggerCooldown.TryGetValue(trig, out var last) && (now - last) < TRIGGER_EPS)
            return false;
        triggerCooldown[trig] = now;
        return true;
    }

    static bool IsAttackClipName(string name)
    {
        var n = name.ToUpperInvariant();
        return n.Contains("ATTACK") || n.Contains("ATK") || n.Contains("SLASH") || n.Contains("HIT") || n.Contains("2_ATTACK");
    }

    // 비주얼
    void ApplyVisualSnapshotByPath()
    {
        if (visualRoot == null && transform != null)
        {
            var maybe = transform.Find("Root");
            if (maybe) visualRoot = maybe;
        }

        if (visualRoot == null || tape == null || tape.visualParts == null || tape.visualParts.Count == 0)
            return;

        if (spriteCache == null) spriteCache = new Dictionary<string, Sprite>(128);

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
        if (spriteCache.TryGetValue(name, out var s) && s != null) return s;

        var all = Resources.FindObjectsOfTypeAll<Sprite>();
        for (int i = 0; i < all.Length; i++)
        {
            var sp = all[i];
            if (sp != null && string.Equals(sp.name, name, StringComparison.OrdinalIgnoreCase))
            {
                spriteCache[name] = sp;
                return sp;
            }
        }
        spriteCache[name] = null;
        return null;
    }
}
