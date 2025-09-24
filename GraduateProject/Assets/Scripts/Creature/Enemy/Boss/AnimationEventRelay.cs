using UnityEngine;

/// 애니메이션 이벤트를 단일 AttackHitbox와 BossController에 전달
public class AnimationEventRelay : MonoBehaviour
{
    private BossController boss;

    [Header("Single Hitbox (optional but recommended)")]
    public AttackHitbox hitbox; // 자식 Hitbox 할당

    void Awake()
    {
        boss = GetComponentInParent<BossController>();
        if (!hitbox) hitbox = GetComponentInChildren<AttackHitbox>();
    }

    // ---- 히트 윈도우 ----
    public void AE_HitBegin() { hitbox?.BeginWindow(); }
    public void AE_HitEnd() { hitbox?.EndWindow(); }

    // ---- 페이로드 실시간 변경(선택) ----
    public void AE_SetDamage(float dmg) { hitbox?.SetPayload(dmg); }
    public void AE_SetKnockbackX(float x) { if (hitbox) hitbox.SetKnockback(x, hitbox ? hitbox.GetComponent<AttackHitbox>().knockback.y : 0f); }
    public void AE_SetKnockback(float x, float y) { hitbox?.SetKnockback(x, y); }

    // ---- 기타 신호 ----
    public void AE_Hit() { boss?.OnAE_Hit(); }
    public void AE_Spawn(string id) { boss?.OnAE_Spawn(id); }
    public void AE_InvulnOn() { boss?.OnAE_Invuln(true); }
    public void AE_InvulnOff() { boss?.OnAE_Invuln(false); }
    public void AE_PhaseGate() { boss?.OnAE_PhaseGate(); }
}
