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
    public UnityEvent onBossCleared;            // 필요시 외부 매니저 호출

    [Header("Scene Unload (optional)")]
    public bool unloadThisSceneOnClear = false; // 클리어 후 보스 씬 언로드
    public float unloadDelay = 2.0f;

    private BossBattleState state = BossBattleState.Idle;
    private HealthController bossHp;
    private Room _ownerRoom;
    void Start()
    {
        // 1) 게이트 닫기
        SetGates(true);

        // 2) 보스 참조 확보 (배치 or 스폰)
        if (!bossInstance)
            if (bossSpawner)
                bossInstance = bossSpawner.Spawn();

        if (!bossInstance)
        {
            Debug.LogError("[BossBattleDirector] BossController를 찾을 수 없습니다.");
            return;
        }
        _ownerRoom = GetComponentInParent<Room>();
        // 3) HP 이벤트 구독
        bossHp = bossInstance.GetComponent<HealthController>();
        if (bossHp != null) bossHp.OnDead += OnBossDead;

        // 4) 전투 시작
        state = BossBattleState.Fighting;

        // === [ADD] Echo Runner 시작 지점 ===
        // PlayerController는 PlayerManager의 UnitRoot에 붙어있는 구조(프로젝트 기준).
        var pm = GameManager.Instance?.PlayerManager;
        var playerRoot = pm != null ? pm.UnitRoot : null;
        var playerController = playerRoot ? playerRoot.GetComponent<PlayerController>() : null;
        if (playerController == null)
        {
            // 최후의 보강: 씬에서 PlayerController 직접 탐색
            playerController = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
        }
        if (playerController != null && EchoManager.I != null)
        {
            EchoManager.I.BeginBossBattle(playerController);
        }
        else
        {
            Debug.LogWarning("[BossBattleDirector] EchoManager.BeginBossBattle 실패: PlayerController 또는 EchoManager를 찾지 못함");
        }
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

        // Echo Runner 종료(클리어)
        if (EchoManager.I != null)
            EchoManager.I.EndBossBattle(playerDied: false);

        // 게이트 열기
        SetGates(false);

        // ★ 보스 사망 = 바로 스테이지 클리어 처리 알림
        GameManager.Instance?.OnBossCleared(_ownerRoom);

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
