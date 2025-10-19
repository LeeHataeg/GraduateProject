// StatController.cs
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using static Define;

[DefaultExecutionOrder(-100)]   // ★ HealthController보다 일찍 실행되게
public class StatController : MonoBehaviour, ICombatStatHolder
{
    [Header("Base Stat (SO)")]
    [SerializeField] private CombatStatSheet baseSheet; // 인스펙터에 넣어둔 SO
    private CombatStatSheet runtime; // 런타임 사본

    public CombatStatSheet Stats => runtime;

    private void Awake()
    {
        // 1) SO가 있으면 사본 생성
        if (baseSheet != null)
            runtime = Instantiate(baseSheet);

        // 2) SO가 없다면, 같은 GO에 컴포넌트형 시트가 붙어 있을 수도 있으니 시도
        if (runtime == null)
            runtime = GetComponent<CombatStatSheet>();

        // 3) 그래도 없으면 최소한의 빈 시트 생성(방어)
        if (runtime == null)
        {
            runtime = ScriptableObject.CreateInstance<CombatStatSheet>();
            Debug.LogWarning($"[{nameof(StatController)}] BaseSheet가 없어 빈 스탯을 생성했습니다.");
        }
    }

    // (장비 적용용) 여기서 runtime에 가감/곱 적용
    public void Apply(System.Collections.Generic.IEnumerable<StatModifier> modifiers, int sign)
    {
        if (runtime == null || modifiers == null) return;
        const float PCT = 10000f;

        foreach (var m in modifiers)
        {
            switch (m.stat)
            {
                case StatType.PhysAtk:
                    if (m.method == ModMethod.Flat) runtime.PhysAtk += sign * m.value;
                    else runtime.PhysAtk *= 1f + sign * (m.value / PCT);
                    break;
                case StatType.BaseDmg:
                    if (m.method == ModMethod.Flat) runtime.BaseDmg += sign * m.value;
                    else runtime.BaseDmg *= 1f + sign * (m.value / PCT);
                    break;
                case StatType.MaxHp:
                    // MaxHp를 float로 바꿨다면 아래처럼. (int라면 RoundToInt로 캐스팅)
                    float maxHp = runtime.MaxHp;
                    if (m.method == ModMethod.Flat)
                    {
                        maxHp += sign * m.value;
                        runtime.MaxHp = (int)maxHp;
                    }
                    else
                    {
                        maxHp *= (1f + sign * (m.value / PCT));
                        runtime.MaxHp = (int)maxHp;
                    }
                    break;
                    // TODO: 나머지 스탯 추가
            }
        }
    }

    public float CalculatePhysicsDmg()
    {
        // 런타임 시트 우선, 없으면 baseSheet(혹은 최소 1f 리턴)
        var s = runtime != null ? runtime : baseSheet;
        if (s == null) return 1f;

        // 기본 물리 데미지: (PhysAtk + BaseDmg) × 랜덤(±10%)
        float dmg = (s.PhysAtk + s.BaseDmg) * UnityEngine.Random.Range(0.9f, 1.1f);

        // 크리티컬 적용: 확률 체크 후 배수 곱
        if (UnityEngine.Random.value < s.CriticalChance)
            dmg *= Mathf.Max(1f, s.CriticalDamage);

        // 음수/0 방어 (이상치 대비)
        return Mathf.Max(1f, dmg);
    }

}
