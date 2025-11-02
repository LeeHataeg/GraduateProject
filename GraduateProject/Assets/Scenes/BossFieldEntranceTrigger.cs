using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class BossFieldEntranceTrigger : MonoBehaviour
{
    [Header("Boss Field Scene")]
    public string bossFieldSceneName = "BossField_Orc";

    [Header("Player Spawn in Boss Scene")]
    public string playerSpawnTagInBossScene = "BossPlayerSpawn"; // Boss 씬에 이 태그의 트랜스폼 하나 두기

    [Header("Options")]
    public bool oneShot = true;        // 한 번만 진입
    public bool setActiveScene = true; // Additive 로드 후 활성 씬 전환
    public bool tryMovePlayerIfRoot = false; // 루트일 때만 씬 이동 시도 (기본: 안 옮김)

    private bool entered;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (entered && oneShot) return;
        if (!other.CompareTag("Player")) return;

        entered = true;
        StartCoroutine(LoadBossFieldAndMovePlayer());
    }

    private IEnumerator LoadBossFieldAndMovePlayer()
    {
        // 0) 필수 레퍼런스
        var gm = GameManager.Instance;
        var pm = gm != null ? gm.PlayerManager : null;
        if (pm == null || pm.Player == null || pm.UnitRoot == null)
        {
            Debug.LogError("[BossFieldEntranceTrigger] PlayerManager/Player/UnitRoot not ready.");
            yield break;
        }

        // 1) 보스 씬 Additive 로드
        var load = SceneManager.LoadSceneAsync(bossFieldSceneName, LoadSceneMode.Additive);
        while (!load.isDone) yield return null;

        var bossScene = SceneManager.GetSceneByName(bossFieldSceneName);
        if (!bossScene.IsValid())
        {
            Debug.LogError($"[BossFieldEntranceTrigger] Scene '{bossFieldSceneName}' invalid.");
            yield break;
        }

        // 2) 활성 씬 전환(선택)
        if (setActiveScene)
            SceneManager.SetActiveScene(bossScene);

        // 3) (선택) 플레이어 씬 이동: "루트 GameObject"이고 DDOL이 아닐 때만
        //    기본은 이동하지 않고 동일 씬에 둠(충분히 정상 동작).
        if (tryMovePlayerIfRoot)
        {
            bool isInDDOL = pm.Player.scene.name == "DontDestroyOnLoad";
            bool isRoot = pm.Player.transform.parent == null; // 루트 여부
            if (!isInDDOL && isRoot && pm.Player.scene != bossScene)
            {
                // 이 경우에만 이동 허용. (부모가 있으면 ArgumentException 발생)
                SceneManager.MoveGameObjectToScene(pm.Player, bossScene);
            }
            else
            {
                Debug.Log($"[BossFieldEntranceTrigger] Skip moving Player. isDDOL={isInDDOL}, isRoot={isRoot}, sameScene={(pm.Player.scene == bossScene)}");
            }
        }

        // 4) 스폰 지점으로 텔레포트 (UnitRoot만 위치/속도 처리)
        Transform spawn = FindSpawnInScene(bossScene, playerSpawnTagInBossScene);
        if (spawn != null)
        {
            var unit = pm.UnitRoot;
            var rb = unit.GetComponent<Rigidbody2D>();
#if UNITY_6000_0_OR_NEWER
            if (rb) rb.linearVelocity = Vector2.zero;
#else
            if (rb) rb.velocity = Vector2.zero;
#endif
            unit.transform.position = spawn.position;
        }
        else
        {
            Debug.LogWarning($"[BossFieldEntranceTrigger] Spawn tag '{playerSpawnTagInBossScene}' not found in '{bossFieldSceneName}'.");
        }

        // 5) (선택) Echo Runner: 보스전 시작 시점 명확화
        var pc = pm.UnitRoot.GetComponent<PlayerController>();
        if (pc != null && EchoManager.I != null)
        {
            EchoManager.I.BeginBossBattle(pc);
        }
    }

    private Transform FindSpawnInScene(Scene scene, string tag)
    {
        if (!scene.IsValid()) return null;

        foreach (var root in scene.GetRootGameObjects())
        {
            var trs = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in trs)
                if (t.CompareTag(tag)) return t;
        }
        return null;
    }
}
