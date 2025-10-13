using UnityEngine;
using UnityEngine.SceneManagement;

public class ClearPanelUI : MonoBehaviour
{
    [Header("Scene Names")]
    [Tooltip("처음으로(메인 메뉴 등) 가는 씬 이름")]
    public string startSceneName = "StartScene";

    [Tooltip("인게임 기본 씬 이름")]
    public string inGameSceneName = "InGameScene";

    [Header("Options")]
    [Tooltip("버튼 클릭 시 Time.timeScale을 1로 복구")]
    public bool normalizeTimeScale = true;

    // === 버튼용 ===
    public void OnClickRestart()
    {
        if (normalizeTimeScale) Time.timeScale = 1f;

        // 선택: 매니저 리셋 훅
        TrySoftResetManagers();

        // 단일 모드 로드로 모든 애디티브/보스필드 정리
        LoadSingle(inGameSceneName);
    }

    public void OnClickHome()
    {
        if (normalizeTimeScale) Time.timeScale = 1f;

        // 선택: 매니저 리셋 훅
        TrySoftResetManagers();

        // 인게임 메인 씬으로 단일 모드 로드
        LoadSingle(startSceneName);
    }

    // === 내부 ===
    private void LoadSingle(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[ClearPanelUI] Scene name is empty.");
            return;
        }

        // 단일 모드: 이미 로드된 애디티브 씬(보스필드 등) 자동 정리
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    /// 매니저 초기화(있으면 호출, 없으면 무시)
    private void TrySoftResetManagers()
    {
        // GameManager가 있다면, 자주 필요한 정리 코드 예시
        var gm = FindAnyObjectByType<GameManager>();
        if (gm != null)
        {
            // 필요 시 여기에 네가 만든 초기화 메서드를 넣어줘.
            // 예) gm.UIManager?.HideAll();
            //     gm.PoolManager?.ClearAll();
            //     gm.RoomManager?.ResetRooms();
            //     gm.PlayerManager?.ResetState();
        }
    }
}
