using System.Collections;
using UnityEngine;

/// 단일 애니메이션만 재생하는 무브.
/// 데미지/판정/스폰 등은 전부 AnimationEvent + AttackHitbox/DamageArea로 처리한다.
[CreateAssetMenu(menuName = "Boss/Moves/Simple (Play One Anim)")]
public class SimpleAnimMoveSO : BossMoveSO
{
    protected override IEnumerator Execute(BossContext ctx)
    {
        // 이 무브는 애니만 재생하고 별도 로직 없음.
        // windup/recover/cooldown/lockMovement은 부모(BossMoveSO)가 이미 처리.
        yield break;
    }
}
