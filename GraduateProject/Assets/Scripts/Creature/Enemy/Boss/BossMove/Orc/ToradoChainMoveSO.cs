using System.Collections;
using UnityEngine;
using static Define;

/// P2 전용: ToradoAtkIn → ToradoAtkLoop(true) 유지 → ToradoAtkOut
/// * 데미지는 Loop 동안만 발생
///   - 방법 A: fieldPrefab(DamageArea)로 DPS 주기
///   - 방법 B: 애니메이션 이벤트(Loop 구간에 AE_HitBegin/End)로 AttackHitbox 사용
[CreateAssetMenu(menuName = "Boss/Moves/Torado Chain (In→Loop→Out)")]
public class ToradoChainMoveSO : BossMoveSO
{
    [Header("Anim Keys")]
    public AnimKey inKey = AnimKey.ToradoAtkIn;
    public AnimKey loopKey = AnimKey.ToradoAtkLoop; // Bool 루프
    public AnimKey outKey = AnimKey.ToradoAtkOut;

    [Header("Loop Duration")]
    [Tooltip("Loop 유지 시간(초). 0 이하면 recover 값 사용")]
    public float loopDuration = 6f;

    [Header("(선택) 장판 프리팹")]
    public GameObject fieldPrefab; // DamageArea 포함 프리팹(없으면 이벤트 방식으로만)
    public float fieldLifeTime = 6f;
    public bool parentFieldToBoss = false;

    protected override IEnumerator Execute(BossContext ctx)
    {
        // In
        ctx.Anims.Play(ctx.Anim, inKey);
        yield return new WaitForSeconds(0.15f);

        // Loop on
        ctx.Anims.Play(ctx.Anim, loopKey, true);

        // (선택) 장판 생성 → DamageArea가 Loop 동안만 딜
        GameObject spawned = null;
        if (fieldPrefab)
        {
            var pos = ctx.AttackOrigin ? ctx.AttackOrigin.position : ctx.Self.position;
            spawned = Object.Instantiate(fieldPrefab, pos, Quaternion.identity);
            if (parentFieldToBoss && spawned) spawned.transform.SetParent(ctx.Self);
            if (fieldLifeTime > 0f) Object.Destroy(spawned, fieldLifeTime);
        }

        // Loop 유지
        float hold = (loopDuration > 0f) ? loopDuration : Mathf.Max(0.1f, recover);
        yield return new WaitForSeconds(hold);

        // Loop off
        ctx.Anims.Play(ctx.Anim, loopKey, false);

        // Out
        ctx.Anims.Play(ctx.Anim, outKey);
    }
}
