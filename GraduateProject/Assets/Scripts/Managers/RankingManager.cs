using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StageRecord
{
    public int stageIndex;

    // 마지막 클리어 기록
    public int lastScore;
    public float lastClearTime;
    public int lastEnemyScore;
    public int lastBossScore;
    public int lastEchoCount;

    // 최고 기록
    public int bestScore;
    public float bestClearTime;
    public int bestEnemyScore;
    public int bestBossScore;
    public int bestEchoCount;
}

public class RankingManager : MonoBehaviour
{
    [Header("시간 보너스")]
    public float timeScoreBase = 10000f;

    [Header("적 처치 점수")]
    public int defaultEnemyKillScore = 10;

    [Header("에코가 많을 수록 감점")]
    public int echoPenaltyPerGhost = 200;

    private readonly Dictionary<int, StageRecord> records = new();

    private int curStageInd = -1;
    private float stageStartTime;
    private bool isStageRun;

    private int enemyScore;
    private int bossScore;

    public IReadOnlyDictionary<int, StageRecord> Records => records;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void BeginStage(int stageIndex)
    {
        curStageInd = stageIndex;
        stageStartTime = Time.time;
        isStageRun = true;

        enemyScore = 0;
        bossScore = 0;
    }

    public void OnEnemyKilled(EnemyArchetypeSO archetype)
    {
        if (!isStageRun || curStageInd <= 0) return;

        int add = (archetype != null) ? archetype.killScore : defaultEnemyKillScore;
        if (add < 0) add = 0;

        enemyScore += add;
    }

    public void OnBossKilled(BossDefinitionSO bossDef)
    {
        if (!isStageRun || curStageInd <= 0) return;

        int add = (bossDef != null) ? bossDef.clearScore : 0;
        if (add < 0) add = 0;

        bossScore += add;
    }

    // firebase와 통신
    public void OnStageCleared()
    {
        if (!isStageRun || curStageInd <= 0) return;

        float clearTime = Mathf.Max(0f, Time.time - stageStartTime);
        int enemyScore = this.enemyScore;
        int bossScore = this.bossScore;

        // Echo 개수는 EchoManager에서 가져옴
        int echoCount = 0;
        if (EchoManager.I != null)
            echoCount = Mathf.Max(0, EchoManager.I.LastEchoCount);

        // 시간 점수: clearTime이 짧을수록 크게
        int timeScore = Mathf.RoundToInt(timeScoreBase / Mathf.Max(1f, clearTime));

        // Echo 감점
        int echoPenalty = echoCount * echoPenaltyPerGhost;

        int totalRecord = Mathf.Max(0, timeScore + enemyScore + bossScore - echoPenalty);

        var record = GetOrCreateRecord(curStageInd);
        record.stageIndex = curStageInd;
        record.lastScore = totalRecord;
        record.lastClearTime = clearTime;
        record.lastEnemyScore = enemyScore;
        record.lastBossScore = bossScore;
        record.lastEchoCount = echoCount;

        // 최고 기록 갱신
        if (totalRecord > record.bestScore)
        {
            record.bestScore = totalRecord;
            record.bestClearTime = clearTime;
            record.bestEnemyScore = enemyScore;
            record.bestBossScore = bossScore;
            record.bestEchoCount = echoCount;
        }

        isStageRun = false;

        // Firebase에 최고 거점수 기입 ㄱㄱ
        CompareAndUploadBestScore(curStageInd, record.bestScore, record.bestClearTime);
    }

    private StageRecord GetOrCreateRecord(int stageIndex)
    {
        if (!records.TryGetValue(stageIndex, out var rec))
        {
            rec = new StageRecord { stageIndex = stageIndex };
            records[stageIndex] = rec;
        }
        return rec;
    }

    public StageRecord GetRecord(int stageIndex)
    {
        records.TryGetValue(stageIndex, out var rec);
        return rec;
    }

    public void ResetAllRuntime()
    {
        isStageRun = false;
        curStageInd = -1;
        enemyScore = 0;
        bossScore = 0;
    }

    private void CompareAndUploadBestScore(int stageIndex, int bestScore, float bestTime)
    {
        var auth = FirebaseAuth.DefaultInstance;
        var user = auth.CurrentUser;
        if (user == null)
        {
            Debug.LogWarning("[Ranking] Firebase 업로드 제낌: 유저 정보 없음");
            return;
        }

        var db = FirebaseFirestore.DefaultInstance;
        // 데이터 저장할 경로 지정하여 reference 하나 만듦 
        //  Collection : 최상위 컬렉션
        //  Document 해당 컬렉션 안에서 
        var docRef = db.Collection("leaderboards")
                       .Document($"stage_{stageIndex}")
                       .Collection("scores")
                       .Document(user.UserId);

        var data = new Dictionary<string, object>
        {
            { "userId", user.UserId },
            { "userName", string.IsNullOrEmpty(user.Email) ? user.UserId : user.Email },
            { "bestScore", bestScore },
            { "bestTime", bestTime },
            { "updatedAt", Timestamp.GetCurrentTimestamp() }
        };

        docRef.SetAsync(data, SetOptions.MergeAll).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError($"[Ranking] Firebase 업로드 실패: {task.Exception}");
            }
            else
            {
                Debug.Log("[Ranking] Firebase 업로드 성공");
            }
        });
    }

}
