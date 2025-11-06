using UnityEngine;
using static Define;

[CreateAssetMenu(menuName = "Enemy/Archetype")]
public class EnemyArchetypeSO : ScriptableObject
{
    [Header("Identity")]
    public EnemyKind kind;
    public string displayName;

    [Header("Prefab & Visual")]
    public GameObject prefab;               // 이 프리팹에는 EnemyController, StatController, Health, Animator 등이 붙어있어야 함 (또는 아래 Assembler가 채워줌)

    [Header("Combat")]
    public AttackMode attackMode;
    public CombatStatSheet statSheet;       // 타입별 스탯 시트 (SO)

    [Header("Ranged Only")]
    public SimpleProjectile projectilePrefab;
    public float projectileSpeed = 12f;
    public float projectileLife = 3f;
    public LayerMask projectileHitMask;
    public float attackRangeOverride = 0f;  // 0이면 statSheet.AttackRange / Behavior 기본값 사용

    [Header("Buffer (Shaman)")]
    public bool useBuffAura;
    public float buffRadius = 4f;
    public float buffDuration = 6f;
    public float buffRefreshRate = 1.0f;    // 몇 초마다 재적용 검사
    public float dmgMultiplier = 1.25f;   // 아군 최종 공격력 배수 (예: 1.25 = +25%)
}
