using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

// 플랫폼 오브젝트엔
// - TilemapCollider2D + CompositeCollider2D
// - PlatformEffector2D (Used By Composite 체크)
// - 이 스크립트
[RequireComponent(typeof(Collider2D), typeof(PlatformEffector2D))]
public class PlatformController : MonoBehaviour
{
    [SerializeField] float dropDuration = 0.5f;

    int originalLayer;
    int noPlatformLayer;

    void Awake()
    {
        originalLayer = gameObject.layer;
        noPlatformLayer = LayerMask.NameToLayer("Player_NoPlatform");
    }

    public void Move()
    {
        // 만약 움직이는 플랫폼이라면
    }

    public void Disapear()
    {
        // 밟고 사라지는 플랫폼이라면
    }
}
