using System.Collections;
using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StageSelectUI : MonoBehaviour
{
    [Header("Stage Buttons")]
    [SerializeField] private List<StageButtonUI> stageButtons; // 인스펙터에서 StageButton_1~3 끌어다 놓기

    [Header("Start Button")]
    [SerializeField] private Button startButton;

    private int _selectedStage = 1;

    private void Awake()
    {
        if (startButton != null)
            startButton.onClick.AddListener(OnClickStart);
    }

    private void OnEnable()
    {
        // 패널 켜질 때마다 유저 진행도 로딩
        StartCoroutine(Co_LoadStageProgressAndSetup());
    }

    private IEnumerator Co_LoadStageProgressAndSetup()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.Stages == null || gm.Stages.Count == 0)
            yield break;

        int totalStages = gm.Stages.Count;

        // 기본값: 1스테지만 열려있고 나머지는 잠김
        int maxClearedStage = 0;

        var auth = FirebaseAuth.DefaultInstance;
        var user = auth.CurrentUser;

        if (user != null)
        {
            var db = FirebaseFirestore.DefaultInstance;
            string uid = user.UserId;

            // 1~N 까지 돌면서 "이 유저가 이 스테이지 클리어했는지" 체크
            for (int i = 1; i <= totalStages; i++)
            {
                var docRef = db.Collection("leaderboards")
                               .Document($"stage_{i}")
                               .Collection("scores")
                               .Document(uid);

                var task = docRef.GetSnapshotAsync();
                // Firestore Task를 코루틴에서 기다리기
                while (!task.IsCompleted) yield return null;

                if (!task.IsFaulted && task.Result.Exists)
                {
                    maxClearedStage = i;  // 가장 높은 클리어 스테이지 갱신
                }
            }
        }

        // 열어줄 마지막 스테이지: maxClearedStage + 1 (단, 전체 개수 넘어가면 clamp)
        int maxUnlockedStage = Mathf.Clamp(maxClearedStage + 1, 1, totalStages);

        // 버튼 상태 셋업
        foreach (var btn in stageButtons)
        {
            if (btn == null) continue;

            int idx = btn.StageIndex;

            // 인스펙터에서 StageIndex를 안 넣었다면, 여기서 강제로 맞춰 줄 수도 있음
            if (idx <= 0 || idx > totalStages)
                continue;

            bool unlocked = (idx <= maxUnlockedStage);
            btn.SetUnlocked(unlocked);

            // 기본 선택 스테이지는 "가장 높은 열린 스테이지"
            if (unlocked && idx == maxUnlockedStage)
                SetSelectedStage(idx);
        }
    }

    public void SetSelectedStage(int stageIndex)
    {
        _selectedStage = stageIndex;

        // 버튼들 하이라이트 갱신
        foreach (var btn in stageButtons)
        {
            if (btn == null) continue;
            btn.SetSelected(btn.StageIndex == stageIndex);
        }
    }

    private void OnClickStart()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        Debug.Log("[OnClickStart]_selectedStage : " + _selectedStage);

        gm.CurrentStage = _selectedStage;
        // InGameScene 로드
        SceneManager.LoadScene(Const.Scene_InGame);
    }
}
