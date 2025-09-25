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
        AsyncOperation op = SceneManager.LoadSceneAsync(bossFieldSceneName, LoadSceneMode.Additive);
        while (!op.isDone) yield return null;

        // 2) 활성 씬 전환(선택)
        if (setActiveScene)
        {
            var scene = SceneManager.GetSceneByName(bossFieldSceneName);
            if (scene.IsValid()) SceneManager.SetActiveScene(scene);
        }

        // 3) 플레이어 이동(인벤/장비 보존: Player 오브젝트는 Persist로 유지된다고 가정)
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player)
        {
            Transform spawn = FindSpawnInScene(bossFieldSceneName, playerSpawnTagInBossScene);
            if (spawn)
            {
                player.transform.position = spawn.position;
            }
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
