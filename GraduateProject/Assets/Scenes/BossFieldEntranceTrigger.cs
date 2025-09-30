using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class BossFieldEntranceTrigger : MonoBehaviour
{
    [Header("Boss Field Scene")]
    public string bossFieldSceneName = "BossField_Orc";

    [Header("Player Spawn in Boss Scene")]
    public string playerSpawnTagInBossScene = "BossPlayerSpawn"; // Boss 씬에 이 태그의 트랜스폼을 하나 둬라

    [Header("Options")]
    public bool oneShot = true;        // 한 번만 진입 허용
    public bool setActiveScene = true; // Additive 로드 후 활성 씬으로 전환

    private bool entered;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (entered && oneShot) return;
        if (!other.CompareTag("Player")) return;

        entered = true;
        StartCoroutine(LoadBossFieldAndMovePlayer());
    }

    private IEnumerator LoadBossFieldAndMovePlayer()
    {
        // 1) Additive 로드
        var load = SceneManager.LoadSceneAsync(bossFieldSceneName, LoadSceneMode.Additive);
        while (!load.isDone) yield return null;

        // 2) (선택) 활성 씬 전환
        var bossScene = SceneManager.GetSceneByName(bossFieldSceneName);
        if (setActiveScene && bossScene.IsValid())
            SceneManager.SetActiveScene(bossScene);

        // 3) 플레이어 루트 찾기 (중요: 루트여야 Move 가능)
        var any = GameObject.FindGameObjectWithTag("Player");
        if (any == null) yield break;
        /*var playerRoot = any.transform.Find("UnitRoot").gameObject;*/  // ★ 루트 강제

        // 4) DDOL 여부 확인
        bool isInDDOL = any.scene.name == "DontDestroyOnLoad";

        // 5) 보스 씬으로 소속 이동 (DDOL이면 Move 불가 → 스킵)
        if (!isInDDOL && bossScene.IsValid() && any.scene != bossScene)
        {
            // 루트가 아니면 부모 분리 (안전)
            if (any.transform.parent != null)
                any.transform.SetParent(null, true);

            SceneManager.MoveGameObjectToScene(any, bossScene);
        }

        // 6) 스폰 포인트로 텔레포트
        Transform spawn = FindSpawnInScene(bossFieldSceneName, playerSpawnTagInBossScene);
        if (spawn != null)
        {
            var rb = any.GetComponent<Rigidbody2D>();
            if (rb) rb.linearVelocity = Vector2.zero;   // Unity 6 권장
            any.transform.position = spawn.position;
        }
    }


    private Transform FindSpawnInScene(string sceneName, string tag)
    {
        var scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.IsValid()) return null;

        foreach (var root in scene.GetRootGameObjects())
        {
            var tagged = root.GetComponentInChildren<TransformWithTag>(true);
            if (tagged && tagged.CompareTag(tag)) return tagged.transform;

            // 일반 Tag 탐색
            var tr = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in tr)
                if (t.CompareTag(tag)) return t;
        }
        return null;
    }

    // 태그가 Transform에 직접 없을 때 대비용 더미
    private class TransformWithTag : MonoBehaviour { }
}
