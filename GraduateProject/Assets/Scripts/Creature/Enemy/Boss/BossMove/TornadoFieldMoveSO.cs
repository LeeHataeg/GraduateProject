using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Boss/Moves/Tornado Field")]
public class TornadoFieldMoveSO : BossMoveSO
{
    public GameObject fieldPrefab;
    public float lifeTime = 6f;
    protected override IEnumerator Execute(BossContext ctx)
    {
        var go = GameObject.Instantiate(fieldPrefab, ctx.AttackOrigin.position, Quaternion.identity);
        GameObject.Destroy(go, lifeTime);
        yield break;
    }
}