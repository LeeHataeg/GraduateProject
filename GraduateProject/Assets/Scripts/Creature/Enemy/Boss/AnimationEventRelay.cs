using UnityEngine;

/// 애니메이션 이벤트로부터 BossController/Move에 신호를 전달하는 얇은 리시버
public class AnimationEventRelay : MonoBehaviour
{
    private BossController boss;

    void Awake() { boss = GetComponentInParent<BossController>(); }

    // 애니메이션에서 호출: 타격 타이밍
    public void AE_Hit() { boss?.OnAE_Hit(); }

    // 특정 프리팹 스폰
    public void AE_Spawn(string id) { boss?.OnAE_Spawn(id); }

    // 무적 On/Off
    public void AE_InvulnOn() { boss?.OnAE_Invuln(true); }
    public void AE_InvulnOff() { boss?.OnAE_Invuln(false); }

    // 페이즈 전환 게이트
    public void AE_PhaseGate() { boss?.OnAE_PhaseGate(); }
}
