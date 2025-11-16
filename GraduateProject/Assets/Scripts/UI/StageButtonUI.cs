using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageButtonUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private Button button;
    [SerializeField] private GameObject lockIcon;  // 잠금 표시용 (선택사항)

    public int StageIndex { get; private set; }

    public void Init(int stageIndex, System.Action<int> onClick)
    {
        StageIndex = stageIndex;

        if (label != null)
            label.text = $"Stage {stageIndex}";

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(stageIndex));
        }
    }

    public void SetUnlocked(bool unlocked)
    {
        if (button != null)
            button.interactable = unlocked;

        if (lockIcon != null)
            lockIcon.SetActive(!unlocked);
    }

    public void SetSelected(bool selected)
    {
        // 선택 효과 (색 바꾸기 등) 넣고 싶으면 여기서 처리
        // 예: 버튼 배경 색, outline 등
    }
}
