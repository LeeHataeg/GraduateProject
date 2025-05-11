using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;

[RequireComponent(typeof(TilemapCollider2D), typeof(PlatformEffector2D))]
public class PlatformController : MonoBehaviour
{
    [SerializeField]
    float dropThroughDuration = 0.5f;

    TilemapCollider2D _collider;
    PlatformEffector2D _effector;

    void Awake()
    {
        // Rigidbody2D 제거: 플랫폼에는 없어야 정상 작동
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            DestroyImmediate(rb);
            Debug.LogWarning("PlatformController: Rigidbody2D가 제거되었습니다.");
        }

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

        var kb = Keyboard.current;
        if (kb == null)
            return;

        if (kb.sKey.isPressed && kb.spaceKey.wasPressedThisFrame)
        {
            StartCoroutine(TemporarilyDisableCollider());
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("Platform collided with: " + collision.gameObject.name);
    }


    IEnumerator TemporarilyDisableCollider()
    {
        _collider.enabled = false;
        yield return new WaitForSeconds(dropThroughDuration);
        _collider.enabled = true;
    }
}
