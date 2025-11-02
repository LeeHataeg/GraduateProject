using System.Collections.Generic;
using UnityEngine;

public class EchoManager : MonoBehaviour
{
    public static EchoManager I { get; private set; }

    [Header("Ghost")]
    public EchoPlayback ghostPrefab;
    [Tooltip("오래된 것→연한 투명도, 최신→진한 투명도")]
    public float[] alphaByAge = { 0.20f, 0.28f, 0.36f, 0.45f, 0.60f };

    EchoRecorder recorder;
    PlayerController player;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── 전투 시작 ──
    public void BeginBossBattle(PlayerController playerController)
    {
        player = playerController;

        // (1) 스태시 아이템 지급(있으면)
        var stash = EchoPersistence.LoadStash();
        foreach (var id in stash) EchoInventoryBridge.TryGiveItemToPlayer(player, id);
        // 지급 후에는 “이번 플레이가 끝날 때까지” 보존하므로 지금은 유지

        // (2) 사망 기록 재생(최대 5)
        var tapes = EchoPersistence.LoadTapes();
        int n = tapes.Count;
        for (int i = 0; i < n; i++)
        {
            var tape = tapes[i];
            var spawnPos = (tape.frames.Count > 0) ? (Vector3)tape.frames[0].pos : player.transform.position;
            var ghost = Instantiate(ghostPrefab, spawnPos, Quaternion.identity);
            ghost.Load(tape);
            int idx = Mathf.Clamp(alphaByAge.Length - (n - i), 0, alphaByAge.Length - 1);
            ghost.SetAlpha(alphaByAge[idx]);
        }

        // (3) 기록 시작(없으면 자동 부착)
        recorder = player.GetComponent<EchoRecorder>();
        if (!recorder) recorder = player.gameObject.AddComponent<EchoRecorder>();
        recorder.BeginRecord();
    }

    // ── 전투 종료 ──
    public void EndBossBattle(bool playerDied)
    {
        EchoTape tape = null;

        // recorder가 파괴/비활성/누락일 수 있으므로 철저히 가드
        try
        {
            if (recorder != null && recorder)
            {
                // EndRecord 내부도 가드하지만, 여기서 한 번 더 안전 확인
                tape = recorder.EndRecord(!playerDied);
            }
        }
        catch
        {
            // 파괴 타이밍 경합 등 예외는 무시하고 tape == null 로 처리
        }
        finally
        {
            // 레퍼런스 정리(다음 라운드에서 오래된 참조 사용 금지)
            recorder = null;
        }

        if (playerDied)
        {
            if (tape != null) EchoPersistence.PushDeathTapeFIFO(tape);
        }
        else
        {
            // 클리어: 모든 사망 기록에서 아이템 1개씩 수확 → 스태시에 저장, 그 후 기록 전체 삭제
            EchoPersistence.HarvestItemsFromAllTapes_ThenClear();
        }

        // 이번 플레이 끝났으니 스태시 비움(“다음 플레이에 지급” 규칙 충족)
        EchoPersistence.SaveStash(new List<string>());
    }
}
