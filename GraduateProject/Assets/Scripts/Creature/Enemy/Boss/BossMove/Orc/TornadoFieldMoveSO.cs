using static Define;
using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "Boss/Moves/Tornado Field")]
public class TornadoFieldMoveSO : BossMoveSO
{
    public AnimKey enterKey = AnimKey.ToradoAtkIn;
    public AnimKey loopBoolKey = AnimKey.ToradoAtkLoop; // Bool 루프
    public AnimKey exitKey = AnimKey.ToradoAtkOut;

    public GameObject fieldPrefab;
    public float lifeTime = 6f;

    protected override IEnumerator Execute(BossContext ctx)
    {
        // In
        ctx.Anims.Play(ctx.Anim, enterKey);
        yield return new WaitForSeconds(0.15f);

        // Loop on + 필드 생성
        ctx.Anims.Play(ctx.Anim, loopBoolKey, true);
        if (fieldPrefab)
        {
            var go = GameObject.Instantiate(fieldPrefab, ctx.AttackOrigin.position, Quaternion.identity);
            GameObject.Destroy(go, lifeTime);
        }

        // 루프 유지 시간은 recover/cooldown으로 조절하거나,
        // AE_Hit/AE_PhaseGate 이벤트로 끊어도 된다. 여기서는 recover까지 루프 유지
        if (recover > 0) yield return new WaitForSeconds(recover);

        // Loop off + Out
        ctx.Anims.Play(ctx.Anim, loopBoolKey, false);
        ctx.Anims.Play(ctx.Anim, exitKey);
    }
}
