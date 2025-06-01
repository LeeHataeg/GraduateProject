using UnityEngine;

public class EffectController : MonoBehaviour
{
    private float dmg;
    private float duration;

    public void SetDmg(float dmg)
    {
        this.dmg = dmg;
    }

    public void SetDuration(float duration)
    {
        this.duration = duration;
    }

    public void OnEnable()
    {
        // TODO-PoolManager 반납으로 개선할 예정
        Destroy(this.gameObject, duration);
    }

    private void OnTriggerEnter2D(Collider2D coll)
    {
        // 적 레이어 체크
        if (coll.gameObject.layer != LayerMask.NameToLayer("Enemies"))
            return;

        // 충돌한 콜라이더가 달린 Rigidbody2D를 통해 스크립트를 찾아본다
        var rb = coll.attachedRigidbody;
        EnemyControllerTemp enemy = null;
        if (rb != null)
            enemy = rb.GetComponent<EnemyControllerTemp>();
        else
            enemy = coll.GetComponentInParent<EnemyControllerTemp>();

        if (enemy == null)
        {
            Debug.LogWarning($"Effect collided with '{coll.name}' but no EnemyControllerTemp found.");
            return;
        }

        // 여기서부터는 enemy가 절대 null 아님
        enemy.Damage(dmg);

        // 이펙트 한 번 적중 후 파괴
        Destroy(gameObject);
    }
}
