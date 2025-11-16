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
    [Header("Time Score")]
    [Tooltip("시간 점수 기본값. clearTime이 짧을수록 TimeScore = base / time 이 커짐")]
    public float timeScoreBase = 10000f;

    [Header("Enemy Score")]
    [Tooltip("EnemyArchetypeSO에 killScore가 비어 있을 때 쓸 기본값")]
    public int defaultEnemyKillScore = 10;

    [Header("Echo Penalty")]
    [Tooltip("보스전에서 사용된 Echo(유령) 한 개당 감점될 점수")]
    public int echoPenaltyPerGhost = 200;

    // stageIndex -> 기록
    private readonly Dictionary<int, StageRecord> _records = new();

    // 현재 진행중인 스테이지 런 정보
    private int _currentStageIndex = -1;
    private float _stageStartTime;
    private bool _stageRunning;

    private int _enemyScoreThisRun;
    private int _bossScoreThisRun;

    public IReadOnlyDictionary<int, StageRecord> Records => _records;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>스테이지 시작 시 호출 (GameManager에서 호출 예정)</summary>
    public void BeginStage(int stageIndex)
    {
        _currentStageIndex = stageIndex;
        _stageStartTime = Time.time;
        _stageRunning = true;

        _enemyScoreThisRun = 0;
        _bossScoreThisRun = 0;

#if UNITY_EDITOR
        Debug.Log($"[Ranking] BeginStage {stageIndex} at t={_stageStartTime:F2}");
#endif
    }

    /// <summary>적 처치 시 EnemyScore를 더해준다.</summary>
    public void OnEnemyKilled(EnemyArchetypeSO archetype)
    {
        if (!_stageRunning || _currentStageIndex <= 0) return;

        int add = (archetype != null) ? archetype.killScore : defaultEnemyKillScore;
        if (add < 0) add = 0;

        _enemyScoreThisRun += add;
    }

    /// <summary>보스 처치 시 BossScore를 더해준다.</summary>
    public void OnBossKilled(BossDefinitionSO bossDef)
    {
        if (!_stageRunning || _currentStageIndex <= 0) return;

        int add = (bossDef != null) ? bossDef.clearScore : 0;
        if (add < 0) add = 0;

        _bossScoreThisRun += add;
    }

    /// <summary>
    /// 스테이지 클리어 시 최종 점수를 계산하고 기록한다.
    /// BossBattleDirector에서 보스 사망 시점에 호출.
    /// </summary>
    public void OnStageCleared()
    {
        if (!_stageRunning || _currentStageIndex <= 0) return;

        float clearTime = Mathf.Max(0f, Time.time - _stageStartTime);
        int enemyScore = _enemyScoreThisRun;
        int bossScore = _bossScoreThisRun;

        // Echo 개수는 EchoManager에서 가져옴
        int echoCount = 0;
        if (EchoManager.I != null)
            echoCount = Mathf.Max(0, EchoManager.I.LastEchoCount);

        // 시간 점수: clearTime이 짧을수록 크게
        int timeScore = Mathf.RoundToInt(timeScoreBase / Mathf.Max(1f, clearTime));

        // Echo 감점
        int echoPenalty = echoCount * echoPenaltyPerGhost;

        int total = Mathf.Max(0, timeScore + enemyScore + bossScore - echoPenalty);

        var record = GetOrCreateRecord(_currentStageIndex);
        record.stageIndex = _currentStageIndex;
        record.lastScore = total;
        record.lastClearTime = clearTime;
        record.lastEnemyScore = enemyScore;
        record.lastBossScore = bossScore;
        record.lastEchoCount = echoCount;

        // 최고 기록 갱신
        if (total > record.bestScore)
        {
            record.bestScore = total;
            record.bestClearTime = clearTime;
            record.bestEnemyScore = enemyScore;
            record.bestBossScore = bossScore;
            record.bestEchoCount = echoCount;
        }

#if UNITY_EDITOR
        Debug.Log(
            $"[Ranking] Stage {_currentStageIndex} cleared.\n" +
            $"  Time   : {clearTime:F2}s ⇒ TimeScore {timeScore}\n" +
            $"  Enemy  : {enemyScore}\n" +
            $"  Boss   : {bossScore}\n" +
            $"  Echoes : {echoCount} ⇒ Penalty {echoPenalty}\n" +
            $"  Total  : {total}  (Best: {record.bestScore})");
#else
        Debug.Log($"[Ranking] Stage {_currentStageIndex} cleared. " +
                  $"Score={total} (time={clearTime:F2}s, enemy={enemyScore}, boss={bossScore}, echoes={echoCount})");
#endif

        _stageRunning = false;

        // ★ Firebase로 최고 기록 업로드
        TryUploadBestScoreToFirebase(_currentStageIndex, record.bestScore, record.bestClearTime);
    }

    private StageRecord GetOrCreateRecord(int stageIndex)
    {
        if (!_records.TryGetValue(stageIndex, out var rec))
        {
            rec = new StageRecord { stageIndex = stageIndex };
            _records[stageIndex] = rec;
        }
        return rec;
    }

    public StageRecord GetRecord(int stageIndex)
    {
        _records.TryGetValue(stageIndex, out var rec);
        return rec;
    }

    /// <summary>런 전체를 리셋하고 싶을 때 사용(필요시)</summary>
    public void ResetAllRuntime()
    {
        _stageRunning = false;
        _currentStageIndex = -1;
        _enemyScoreThisRun = 0;
        _bossScoreThisRun = 0;
    }

    /// <summary>
    /// Firebase Firestore에 최고 점수를 업로드 (로그인 되어 있을 때만).
    /// 컬렉션 경로:
    ///  leaderboards / stage_{stageIndex} / scores / {userId}
    /// </summary>
    private void TryUploadBestScoreToFirebase(int stageIndex, int bestScore, float bestTime)
    {
        var auth = FirebaseAuth.DefaultInstance;
        var user = auth.CurrentUser;
        if (user == null)
        {
            Debug.LogWarning("[Ranking] Firebase 업로드 스킵: 로그인 유저 없음");
            return;
        }

        var db = FirebaseFirestore.DefaultInstance;
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

    // ★ 이후 Firebase 연동 시: _records를 그대로 DTO로 변환해서 업로드하면 됨.
    // public List<StageScoreDTO> BuildBestScoreList() { ... } 이런식으로 확장 가능.
}
