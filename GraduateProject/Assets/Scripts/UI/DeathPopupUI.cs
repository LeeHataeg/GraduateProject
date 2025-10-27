// DeathPopupUI.cs (핵심 부분만)
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DeathPopupUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button homeButton;

    [Header("Scene Names")]
    [SerializeField] private string startSceneName = "StartScene";
    [SerializeField] private string inGameSceneName = "InGameScene";

    [Header("Options")]
    [SerializeField] private bool normalizeTimeScale = true;

    private void Awake()
    {
        if (!root) root = gameObject;
        root.SetActive(false);
        if (restartButton) restartButton.onClick.AddListener(OnClickRestart);
        if (homeButton) homeButton.onClick.AddListener(OnClickHome);
    }

    public void Show()
    {
        root.SetActive(true);

        Time.timeScale = 0f;
    }

    public void Hide()
    {
        if (normalizeTimeScale) Time.timeScale = 1f;
        root.SetActive(false);
    }

    private void OnClickRestart()
    {
        if (normalizeTimeScale) Time.timeScale = 1f;
        TrySoftResetManagers();                      // ★ 상태 정리
        SceneManager.LoadScene(inGameSceneName, LoadSceneMode.Single);
    }

    private void OnClickHome()
    {
        if (normalizeTimeScale) Time.timeScale = 1f;
        TrySoftResetManagers();                      // ★ 상태 정리
        SceneManager.LoadScene(startSceneName, LoadSceneMode.Single);
    }

    // DeathPopupUI.cs 내부
    private void TrySoftResetManagers()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        // 1) 모든 UI 닫기(있다면)
        gm.UIManager?.SendMessage("HideAll", SendMessageOptions.DontRequireReceiver);

        // 2) 방/시작점 리셋
        gm.RoomManager?.ResetRooms(); // 파라미터 없는 오버로드 이미 추가했음

        // 3) 플레이어 상태 리셋(핵심!)
        gm.PlayerManager?.ResetState(true); // ← 새 판에서 신규 스폰 유도

        // 4) 패널 닫기(선택)
        root?.SetActive(false);
    }

}
