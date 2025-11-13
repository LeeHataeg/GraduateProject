using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 이거 걍 Boss랑 같이 쓸 수도?
// + 마나, 공격령 등등의 다양한 어쩌고 적용 저쩌고
public class CharacteCurStateUI : MonoBehaviour
{
    [Header("공통 그거 변수")]
    private HealthController target;    // 스크립트에서 할당해줌 ㅇㅇ
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI sliderCurVal;

    [Header("보스 한정 변수 - (걍 보스 이름)")]
    [SerializeField] private TextMeshProUGUI targetName;

    [Header("디버깅 용")]
    public GameObject targetObj;

    private void Awake()
    {
        slider = GetComponentInChildren<Slider>();
        sliderCurVal = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void OnDisable()
    {
        if(target != null)
        {
            target.OnHealthChanged -= HealthChanged;
        }
    }

    // Boss, Player 둘 다 설정 ㄱㄱ
    public void SetTarget(HealthController target)
    {
        this.target = target;
        targetObj = target.gameObject;

        target.OnHealthChanged += HealthChanged;
        HealthChanged(target.CurrentHp, target.MaxHp);
    }

    public void SetTargetName(string name)
    {
        this.targetName.text = name;
    }

    private void HealthChanged(float curHp, float maxHp)
    {
        Debug.Log("[C-Stage] : " + target.gameObject.name 
            + " 의 hp 변동 " + curHp.ToString("F2") + " / " + maxHp.ToString("F2"));

        slider.value = (curHp/maxHp);
        sliderCurVal.text = curHp.ToString("F2") + " / " + maxHp.ToString("F2");
    }
}