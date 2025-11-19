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

        if (count >= 1 && firstRow != null)
            firstRow.ShowEntry(1, list[0].userName, list[0].bestScore, list[0].bestTime);

        if (count >= 2 && secondRow != null)
            secondRow.ShowEntry(2, list[1].userName, list[1].bestScore, list[1].bestTime);

        if (count >= 3 && thirdRow != null)
            thirdRow.ShowEntry(3, list[2].userName, list[2].bestScore, list[2].bestTime);

        if (myEntry == null || myRank <= 0)
        {
            if (userRow != null) userRow.Hide();
            if (upperRow != null) upperRow.Hide();
            if (emptyRow != null) emptyRow.Hide();
            return;
        }

        if (myRank <= 3)
        {
            if (upperRow != null) upperRow.Hide();
            if (emptyRow != null) emptyRow.Hide();
            if (userRow != null) userRow.Hide();
            return;
        }

        int myIndex = myRank - 1; 
        int upperIndex = myIndex - 1;

        if (upperIndex >= 0 && upperIndex < list.Count && upperRow != null)
        {
            var upper = list[upperIndex];
            upperRow.ShowEntry(upperIndex + 1, upper.userName, upper.bestScore, upper.bestTime);
        }

        if (userRow != null)
        {
            userRow.ShowEntry(myRank, myEntry.userName, myEntry.bestScore, myEntry.bestTime);
        }

        if (emptyRow != null)
        {
            if (upperIndex > 3)
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

        if (lastRunScore > 0 && lastRunScore == bestScore)
        {
            myScoreText.text = $"Best Score : {bestScore}점";
        }
        else if (lastRunScore > 0)
        {
            myScoreText.text = $"{lastRunScore}점";
        }
        else
        {
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
