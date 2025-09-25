using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections;

public enum BossBattleState { Idle, Preparation, Fighting, Cleared, Failed }

public class BossBattleDirector : MonoBehaviour
{
    [Header("Boss Reference (choose one)")]
    public BossController bossInstance;  // 미리 배치한 경우 drag
    public BossSpawner bossSpawner;      // 스폰 사용할 경우 drag

    [Header("Gates / Walls to lock during battle")]
    public GameObject[] gatesToClose;

    [Header("UI")]
    public GameObject clearPanel;               // 클리어 시 켜줄 패널
    public UnityEvent onBossCleared;            // 필요시 외부 매니저 호출

    [Header("Scene Unload (optional)")]
    public bool unloadThisSceneOnClear = false; // 클리어 후 보스 씬 언로드
    public float unloadDelay = 2.0f;

    private BossBattleState state = BossBattleState.Idle;
    private HealthController bossHp;

    void Start()
    {
        // 1) 게이트 닫기
        SetGates(true);

        // 2) 보스 참조 확보 (배치 or 스폰)
        if (!bossInstance && bossSpawner)
            bossInstance = bossSpawner.Spawn();

        if (!bossInstance)
        {
            Debug.LogError("[BossBattleDirector] BossController를 찾을 수 없습니다.");
            return;
        }

        // 3) HP 이벤트 구독
        bossHp = bossInstance.GetComponent<HealthController>();
        if (bossHp != null) bossHp.OnDead += OnBossDead;

        // 4) 전투 시작
        state = BossBattleState.Fighting;
    }

    void OnDestroy()
    {
        if (bossHp != null) bossHp.OnDead -= OnBossDead;
    }

    private void SetGates(bool closed)
    {
        foreach (var g in gatesToClose)
            if (g) g.SetActive(closed);
    }

    private void OnBossDead()
    {
        if (state == BossBattleState.Cleared) return;
        state = BossBattleState.Cleared;

        // 게이트 열기
        SetGates(false);

        // UI 표기
        if (clearPanel) clearPanel.SetActive(true);

        // 외부 이벤트(포탈 생성, 보상 지급 등)
        onBossCleared?.Invoke();

        // (선택) 보스 씬 언로드
        if (unloadThisSceneOnClear)
            StartCoroutine(UnloadThisSceneAfterDelay());
    }

    private IEnumerator UnloadThisSceneAfterDelay()
    {
        yield return new WaitForSeconds(unloadDelay);
        var scene = gameObject.scene;
        if (scene.IsValid())
        {
            AsyncOperation op = SceneManager.UnloadSceneAsync(scene);
            while (!op.isDone) yield return null;
        }
    }
}
