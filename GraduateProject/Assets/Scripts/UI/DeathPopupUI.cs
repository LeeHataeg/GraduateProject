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
        TrySoftResetManagers();
        SceneManager.LoadScene(inGameSceneName, LoadSceneMode.Single);
    }

    private void OnClickHome()
    {
        if (normalizeTimeScale) Time.timeScale = 1f;
        TrySoftResetManagers();
        SceneManager.LoadScene(startSceneName, LoadSceneMode.Single);
    }

    private void TrySoftResetManagers()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        // 1) UI 정리
        gm.UIManager?.SendMessage("HideAll", SendMessageOptions.DontRequireReceiver);

        // 2) 방/시작점 리셋(파괴는 RoomManager가 알아서, Player 파괴 없음)
        gm.RoomManager?.ResetRooms();

        // 3) 플레이어 상태만 리셋(★ 파괴 금지), 다음 씬에서 1회 스폰 요청
        gm.PlayerManager?.ResetState(true);
        gm.PlayerManager?.RequestFreshSpawnNextScene();

        // 4) 패널 닫기
        root?.SetActive(false);
    }
}
