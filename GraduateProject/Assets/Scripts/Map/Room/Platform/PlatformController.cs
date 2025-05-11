using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;   // ← Input System 사용

[RequireComponent(typeof(Tilemap), typeof(TilemapCollider2D), typeof(PlatformEffector2D))]
public class PlatformController : MonoBehaviour
{
    [SerializeField]
    float dropThroughDuration = 0.5f;

    TilemapCollider2D _collider;
    PlatformEffector2D _effector;

    void Awake()
    {
        // Rigidbody2D 추가·설정 (Kinematic)
        var rb = GetComponent<Rigidbody2D>()
                 ?? gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        // PlatformEffector2D 설정
        _collider = GetComponent<TilemapCollider2D>();
        _collider.usedByEffector = true;

        _effector = GetComponent<PlatformEffector2D>();
        _effector.useOneWay = true;
        _effector.surfaceArc = 180f;
        _effector.useOneWayGrouping = true;
    }

    void OnCollisionStay2D(Collision2D other)
    {
        if (!other.collider.CompareTag("Player"))
            return;

        // Input System으로 키 입력 감지
        var kb = Keyboard.current;
        if (kb == null)
            return;

        // 'S' 키 누른 상태에서 'Space'를 이번 프레임에 눌렀다면
        if (kb.sKey.isPressed && kb.spaceKey.wasPressedThisFrame)
        {
            StartCoroutine(TemporarilyDisableCollider());
        }
    }

    IEnumerator TemporarilyDisableCollider()
    {
        _collider.enabled = false;
        yield return new WaitForSeconds(dropThroughDuration);
        _collider.enabled = true;
    }
}
