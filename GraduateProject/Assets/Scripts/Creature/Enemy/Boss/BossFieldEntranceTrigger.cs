// BossFieldEntranceTrigger.cs
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class BossFieldEntranceTrigger : MonoBehaviour
{
    [Header("Options")]
    public bool oneShot = true;
    public bool clearNormalRoomsOnEnter = false; // 보스전 전용 방만 남기고 싶을 때

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
        StartCoroutine(EnterBossFieldRoutine());
    }

    private IEnumerator EnterBossFieldRoutine()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.PlayerManager == null || gm.PlayerManager.UnitRoot == null)
        {
            Debug.LogError("[BossFieldEntranceTrigger] GameManager/Player not ready.");
            yield break;
        }

        // (선택) 일반 방 정리
        if (clearNormalRoomsOnEnter)
            gm.RoomManager?.ResetRooms(true);

        // BossField 프리팹 생성 및 스폰 위치 얻기
        var spawnPos = gm.SpawnBossFieldAndGetSpawnPoint();
        if (!spawnPos.HasValue)
        {
            Debug.LogError("[BossFieldEntranceTrigger] Failed to spawn BossField prefab.");
            yield break;
        }

        // 플레이어 텔레포트
        var unit = gm.PlayerManager.UnitRoot;
        var rb = unit.GetComponent<Rigidbody2D>();
#if UNITY_6000_0_OR_NEWER
        if (rb) rb.linearVelocity = Vector2.zero;
#else
        if (rb) rb.velocity = Vector2.zero;
#endif
        unit.transform.position = spawnPos.Value;

        // (선택) 보스전 시작 훅
        var pc = unit.GetComponent<PlayerController>();
        if (pc != null && EchoManager.I != null)
            EchoManager.I.BeginBossBattle(pc);

        yield return null;
    }
}
