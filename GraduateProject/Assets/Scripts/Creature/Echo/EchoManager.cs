using System.Collections.Generic;
using UnityEngine;

public class EchoManager : MonoBehaviour
{
    #region 변수
    public static EchoManager I { get; private set; }

    [Header("Ghost")]
    public EchoPlayback ghostPrefab;
    [Tooltip("오래된 것→연한 투명도, 최신→진한 투명도")]
    public float[] alphaByAge = { 0.20f, 0.28f, 0.36f, 0.45f, 0.60f };

    EchoRecorder recorder;
    PlayerController player;

    #endregion

    public void BeginBossBattle(PlayerController playerController)
    {
        player = playerController;

        // 직전 클리어 보상 제공
        var stash = EchoPersistence.LoadStash();
        foreach (var id in stash) EchoInventoryBridge.TryGiveItemToPlayer(player, id);

        // 사망 기록 재생
        var tapes = EchoPersistence.LoadTapes();
        int n = tapes.Count;
        for (int i = 0; i < n; i++)
        {
            var tape = tapes[i];
            var spawnPos = (tape.frames.Count > 0) ? (Vector3)tape.frames[0].pos : player.transform.position;

            var ghost = Instantiate(ghostPrefab, spawnPos, Quaternion.identity);
            ghost.Load(tape);
            ghost.AttachVisualFrom(player);

            int idx = Mathf.Clamp(alphaByAge.Length - (n - i), 0, alphaByAge.Length - 1);
            ghost.SetAlpha(alphaByAge[idx]);
        }

        // 기록 시작
        recorder = player.GetComponent<EchoRecorder>();
        if (!recorder) recorder = player.gameObject.AddComponent<EchoRecorder>();
        recorder.BeginRecord();
    }

    public void EndBossBattle(bool playerDied)
    {
        var tape = recorder?.EndRecord(!playerDied);

        if (playerDied)
        {
            if (tape != null) EchoPersistence.PushDeathTapeFIFO(tape);
        }
        else
        {
            // 클리어 보상 수여
            EchoPersistence.HarvestItemsFromAllTapes_ThenClear();
        }

        // 클리어 보상 저장
        EchoPersistence.SaveStash(new List<string>());
    }

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this; DontDestroyOnLoad(gameObject);
    }

}
