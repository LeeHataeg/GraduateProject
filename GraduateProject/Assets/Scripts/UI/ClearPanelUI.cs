using UnityEngine;
using UnityEngine.SceneManagement;

public class ClearPanelUI : MonoBehaviour
{
    [Header("Scene Names")]
    public string startSceneName = "StartScene";
    public string inGameSceneName = "InGameScene";

    [Header("Options")]
    public bool normalizeTimeScale = true;

    public void OnClickRestart()
    {
        if (normalizeTimeScale) Time.timeScale = 1f;
        TrySoftResetManagers();
        SceneManager.LoadScene(inGameSceneName, LoadSceneMode.Single);
    }

    public void OnClickHome()
    {
        if (normalizeTimeScale) Time.timeScale = 1f;
        TrySoftResetManagers();
        SceneManager.LoadScene(startSceneName, LoadSceneMode.Single);
    }

    private void TrySoftResetManagers()
    {
        var gm = FindAnyObjectByType<GameManager>();
        if (gm == null) return;

        // UI 전부 닫기(있으면)
        gm.UIManager?.SendMessage("HideAll", SendMessageOptions.DontRequireReceiver);

        // ★★ 핵심: 다음 회차를 위해 시작점/방 정보 초기화
        gm.RoomManager?.ResetRooms();

        // ★ 플레이어 제거 (카메라 + AudioListener 포함)
        gm.PlayerManager?.DespawnPlayer();

        // ★ 랭킹 런타임도 깨끗하게 하고 싶으면 옵션으로:
        gm.RankingManager?.ResetAllRuntime();

        gm.ResetStageClearFlags();
    }
}
