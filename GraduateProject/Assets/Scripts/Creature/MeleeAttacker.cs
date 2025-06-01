using Unity.VisualScripting;
using UnityEngine;
using static Define;

// 공격 Effect(실질적 공격 담당 오브젝트) 스포너
[RequireComponent(typeof(ICombatStatHolder))]
public class MeleeAttacker : MonoBehaviour, IAttackBehavior
{
    private GameObject effectPrefab;
    private CombatStatSheet stats;

    public float Range => stats != null ? stats.AttackRange : 0f;

    private void Awake()
    {
        // 이펙트 프리팹은 Resources 폴더에 "Prefabs/Melee_Attck_Effect"로 배치했다고 가정
        effectPrefab = Resources.Load<GameObject>("Prefabs/Melee_Attck_Effect");
        var statHolder = GetComponent<ICombatStatHolder>();
        if (statHolder != null)
        {
            stats = statHolder.Stats;
        }
        else
        {
            Debug.LogError($"[{nameof(MeleeAttacker)}] ICombatStatHolder를 찾을 수 없습니다.");
        }
    }

    public void Execute(AttackContext ctx)
    {
        if (effectPrefab == null || stats == null || ctx.Source == null) return;

        // (1) 이펙트 인스턴스화
        var spawned = Instantiate(effectPrefab);
        spawned.transform.position = ctx.Origin;
        spawned.transform.rotation = Quaternion.Euler(ctx.Direction);

        // (2) EffectController를 통해 데미지 전달
        var effectCon = spawned.GetComponent<EffectController>();
        if (effectCon != null)
        {
            effectCon.SetDmg(ctx.Damage);
        }

        // (3) 일정 시간 후 이펙트 제거
        Destroy(spawned, 0.5f); // 이펙트 길이에 맞춰 조정
    }
}
 