using Firebase.Auth;
using Firebase.Firestore;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Firebase.Extensions;
using System;
using UnityEngine.UI;

[System.Serializable]
public class LeaderboardEntry
{
    public string userId;
    public string userName;
    public int bestScore;
    public float bestTime;
}

public class RankingUI : MonoBehaviour
{
    [Header("Rows")]
    [SerializeField] private RankingRowUI firstRow;
    [SerializeField] private RankingRowUI secondRow;
    [SerializeField] private RankingRowUI thirdRow;
    [SerializeField] private RankingRowUI upperRow;   // 현재 유저 바로 위
    [SerializeField] private RankingRowUI userRow;    // 현재 유저
    [SerializeField] private RankingRowUI emptyRow;   // ~ ~ ~ 표시용

    [Header("현재 유저 점수 텍스트")]
    [SerializeField] private TextMeshProUGUI myScoreText;

    [Header("Optional: 이 패널이 닫힌 후 켜질 패널 (예: ClearPanelUI)")]
    [SerializeField] private GameObject nextPanel;

    [Header("Close 버튼")]
    [SerializeField] private Button closeButton;

    int stageIndex;
    int lastRunScore = -1;     // 이번에 클리어한 점수 (없으면 -1)

    void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(OnClickClose);
    }

    /// <summary>
    /// 해당 스테이지의 랭킹 UI 활성화.
    /// lastRunScore: 방금 클리어한 점수(없으면 -1)
    /// </summary>
    public void OpenForStage(int stageIndex, int lastRunScore = -1)
    {
        Debug.Log("켜져용");

        this.stageIndex = stageIndex;
        this.lastRunScore = lastRunScore;

        gameObject.SetActive(true);

        ClearAllRows();
        LoadLeaderboardFromFirebase();
    }

    void ClearAllRows()
    {
        if (firstRow != null) firstRow.Hide();
        if (secondRow != null) secondRow.Hide();
        if (thirdRow != null) thirdRow.Hide();
        if (upperRow != null) upperRow.Hide();
        if (userRow != null) userRow.Hide();
        if (emptyRow != null) emptyRow.Hide();
    }

    void LoadLeaderboardFromFirebase()
    {
        var auth = FirebaseAuth.DefaultInstance;
        var user = auth.CurrentUser;
        if (user == null)
        {
            if (myScoreText != null)
                myScoreText.text = "로그인한 유저가 없습니다.";
            return;
        }

        var db = FirebaseFirestore.DefaultInstance;
        var scoresCol = db.Collection("leaderboards")
                          .Document($"stage_{stageIndex}")
                          .Collection("scores");

        // ★ 인덱스 이슈 피하려고 bestScore만 정렬 기준으로 사용
        scoresCol.OrderByDescending("bestScore")
                 .Limit(100)
                 .GetSnapshotAsync()
                 .ContinueWithOnMainThread(task =>
                 {
                     if (task.IsFaulted)
                     {
                         Debug.LogError("[RankingUI] 랭킹 불러오기 실패: " + task.Exception);
                         if (myScoreText != null)
                             myScoreText.text = "랭킹 불러오기 실패";
                         return;
                     }

                     var snap = task.Result;
                     List<LeaderboardEntry> entries = new List<LeaderboardEntry>();

                     int rank = 1;
                     int myRank = -1;
                     LeaderboardEntry myEntry = null;

                     foreach (var doc in snap.Documents)
                     {
                         var dict = doc.ToDictionary();

                         var entry = new LeaderboardEntry
                         {
                             userId = dict.ContainsKey("userId") ? dict["userId"].ToString() : doc.Id,
                             userName = dict.ContainsKey("userName") ? dict["userName"].ToString() : doc.Id,
                             bestScore = dict.ContainsKey("bestScore") ? Convert.ToInt32(dict["bestScore"]) : 0,
                             bestTime = dict.ContainsKey("bestTime") ? Convert.ToSingle(dict["bestTime"]) : 0f,
                         };

                         entries.Add(entry);

                         if (entry.userId == user.UserId)
                         {
                             myRank = rank;
                             myEntry = entry;
                         }

                         rank++;
                     }

                     ApplyRows(entries, myEntry, myRank);
                     UpdateMyScoreText(myEntry);
                 });
    }

    void ApplyRows(List<LeaderboardEntry> list, LeaderboardEntry myEntry, int myRank)
    {
        ClearAllRows();

        int count = list.Count;

        // 1~3등 표시
        if (count >= 1 && firstRow != null)
            firstRow.ShowEntry(1, list[0].userName, list[0].bestScore, list[0].bestTime);

        if (count >= 2 && secondRow != null)
            secondRow.ShowEntry(2, list[1].userName, list[1].bestScore, list[1].bestTime);

        if (count >= 3 && thirdRow != null)
            thirdRow.ShowEntry(3, list[2].userName, list[2].bestScore, list[2].bestTime);

        // 유저 기록이 없다면 (랭킹에 아직 없음) -> 여기서 끝
        if (myEntry == null || myRank <= 0)
        {
            if (userRow != null) userRow.Hide();
            if (upperRow != null) upperRow.Hide();
            if (emptyRow != null) emptyRow.Hide();
            return;
        }

        // 유저가 3등 이내라면 이미 위에서 표시됨.
        // (원하면 해당 Row에 하이라이트 효과 넣을 수 있음)
        if (myRank <= 3)
        {
            if (upperRow != null) upperRow.Hide();
            if (emptyRow != null) emptyRow.Hide();
            if (userRow != null) userRow.Hide();   // 별도 줄은 숨김
            return;
        }

        // 여기서부터 myRank > 3인 경우
        int myIndex = myRank - 1;      // 0-based
        int upperIndex = myIndex - 1;  // 바로 위 사람

        // UpperRecords: 나보다 한 등수 위 유저
        if (upperIndex >= 0 && upperIndex < list.Count && upperRow != null)
        {
            var upper = list[upperIndex];
            upperRow.ShowEntry(upperIndex + 1, upper.userName, upper.bestScore, upper.bestTime);
        }

        // UserRecord: 내 기록
        if (userRow != null)
        {
            userRow.ShowEntry(myRank, myEntry.userName, myEntry.bestScore, myEntry.bestTime);
        }

        // EmptyRecords: 
        // UpperRecords가 4등이 아니면 (즉, 3등과 UpperRecords 사이에 1명 이상 있음) -> ~ ~ ~ 표시
        if (emptyRow != null)
        {
            // upperIndex 는 0-based, 3등은 index 2, 4등은 index 3
            if (upperIndex > 3)   // upperRank >= 5
                emptyRow.ShowEllipsis();
            else
                emptyRow.Hide();
        }
    }

    void UpdateMyScoreText(LeaderboardEntry myEntry)
    {
        if (myScoreText == null) return;

        if (myEntry == null)
        {
            myScoreText.text = "기록 없음";
            return;
        }

        int bestScore = myEntry.bestScore;

        // lastRunScore == 이번에 클리어한 점수
        if (lastRunScore > 0 && lastRunScore == bestScore)
        {
            // 이번 런이 최고 기록
            myScoreText.text = $"Best Score : {bestScore}점";
        }
        else if (lastRunScore > 0)
        {
            // 이번 런이 최고는 아님
            myScoreText.text = $"{lastRunScore}점";
        }
        else
        {
            // 그냥 내 최고 기록만 보여줄 때
            myScoreText.text = $"Best Score : {bestScore}점";
        }
    }

    public void OnClickClose()
    {
        gameObject.SetActive(false);

        if (GameManager.Instance.isFinal)
        {
            GameManager.Instance.UIManager.ShowClearPanel();
        }
    }
}
