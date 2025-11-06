using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 포탈 프리팹에 부착: 스폰 시 벽과 겹치면 격자 단위로 "가까운 안전 위치"로 옮기거나,
/// 최후 수단으로 Wall 타일을 문 너비만큼 카빙해서 자리를 확보한다.
/// </summary>
[DefaultExecutionOrder(10)]
[DisallowMultipleComponent]
public class PortalAutoAlign2D : MonoBehaviour
{
    [Header("Collision Check")]
    [Tooltip("벽/지형 등 포탈이 겹치면 안 되는 레이어(예: Wall, Ground)")]
    public LayerMask blockMask;

    [Tooltip("포탈의 Overlap 체크에 사용할 박스 크기. 비우면 BoxCollider2D/Collider2D에서 자동 추정")]
    public Vector2 overlapBoxSizeOverride;

    [Tooltip("탐색 격자 간격(타일 크기). 보통 1칸")]
    public float step = 1f;

    [Tooltip("최대 탐색 반경(칸 수). 커질수록 더 멀리까지 자리 찾음")]
    public int maxSteps = 3;

    [Header("Carve (Optional)")]
    [Tooltip("안전 위치가 없을 때 마지막 수단으로 Wall 타일을 깎아낼지")]
    public bool allowCarveWall = true;

    [Tooltip("카빙할 타일맵 이름(비우면 모든 Tilemap 대상). 보통 \"Wall\"")]
    public string wallTilemapName = "Wall";

    [Tooltip("카빙 영역(타일 단위). 가로×세로")]
    public Vector2Int carveSize = new Vector2Int(2, 3);

    [Tooltip("포탈 기준 카빙 구역의 하단 여유(발 밑 공간)")]
    public int carveBottomPadding = 0;

    [Header("Debug")]
    public bool log = false;
    public Color gizmoColor = new Color(1, 0.6f, 0.2f, 0.25f);

    private Collider2D _col;
    private Vector2 _boxSize;

    private void Awake()
    {
        _col = GetComponent<Collider2D>();
        _boxSize = overlapBoxSizeOverride;
        if (_boxSize == Vector2.zero)
        {
            if (_col is BoxCollider2D box)
                _boxSize = box.size;
            else
            {
                var b = _col != null ? _col.bounds : new Bounds(transform.position, Vector3.one * 1f);
                _boxSize = b.size;
            }
        }
    }

    private void Start()
    {
        TryRelocateOrCarve();
    }

    private void TryRelocateOrCarve()
    {
        if (!IsBlockedAt(transform.position))
        {
            if (log) Debug.Log("[PortalAutoAlign2D] Already clear.", this);
            return;
        }

        // 1) 격자 탐색(나선형)으로 가장 가까운 "비충돌" 위치 찾기
        if (TrySpiralSearch(out var safePos))
        {
            if (log) Debug.Log($"[PortalAutoAlign2D] Moved to safe pos {safePos}", this);
            transform.position = safePos;
            return;
        }

        // 2) 마지막 수단: Wall 타일 카빙
        if (allowCarveWall && TryCarveHere(transform.position))
        {
            if (log) Debug.Log("[PortalAutoAlign2D] Carved wall under portal.", this);
            return;
        }

        if (log) Debug.LogWarning("[PortalAutoAlign2D] Failed to clear space for portal.", this);
    }

    private bool IsBlockedAt(Vector3 worldPos)
    {
        var hit = Physics2D.OverlapBox(worldPos, _boxSize, 0f, blockMask);
        return hit != null;
    }

    private bool TrySpiralSearch(out Vector3 safePos)
    {
        safePos = transform.position;

        // 검사 순서: (0,0) 제외하고 4방향 → 8방향 확장 형태
        var dirs = new Vector2Int[]
        {
            new(1,0), new(-1,0), new(0,1), new(0,-1),
            new(1,1), new(1,-1), new(-1,1), new(-1,-1)
        };

        for (int r = 1; r <= maxSteps; r++)
        {
            foreach (var d in dirs)
            {
                var offset = new Vector2(d.x * step * r, d.y * step * r);
                var p = (Vector2)transform.position + offset;
                if (!IsBlockedAt(p))
                {
                    safePos = p;
                    return true;
                }
            }
        }
        return false;
    }

    private bool TryCarveHere(Vector3 worldPos)
    {
        // 상위에서 Wall 타일맵 찾기
        var maps = GetComponentsInParent<Tilemap>(true);
        Tilemap wall = null;

        if (!string.IsNullOrEmpty(wallTilemapName))
        {
            foreach (var m in maps)
            {
                if (!m) continue;
                if (m.gameObject.name.Equals(wallTilemapName, System.StringComparison.OrdinalIgnoreCase))
                {
                    wall = m; break;
                }
            }
        }
        if (!wall && maps.Length > 0)
        {
            // 이름 필터가 없거나 못 찾았으면, 가장 “벽스러워 보이는” 타일맵을 하나 고른다(마지막 수단)
            wall = maps[0];
        }
        if (!wall) return false;

        // 월드 → 셀 변환
        var centerCell = wall.WorldToCell(worldPos);
        var halfW = Mathf.Max(1, carveSize.x / 2);
        var height = Mathf.Max(1, carveSize.y);

        bool carvedAny = false;
        for (int x = -halfW + 1; x <= halfW; x++)
        {
            for (int y = carveBottomPadding; y < height + carveBottomPadding; y++)
            {
                var c = new Vector3Int(centerCell.x + x, centerCell.y + y, 0);
                if (wall.HasTile(c))
                {
                    wall.SetTile(c, null);
                    carvedAny = true;
                }
            }
        }
        if (carvedAny)
        {
            wall.CompressBounds();
            // 카빙 후 충돌도 해결되는지 재확인
            if (!IsBlockedAt(worldPos)) return true;
        }
        return false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = gizmoColor;
        var size = overlapBoxSizeOverride;
        if (size == Vector2.zero && TryGetComponent<Collider2D>(out var c))
        {
            if (c is BoxCollider2D b) size = b.size;
            else size = c.bounds.size;
        }
        if (size == Vector2.zero) size = Vector2.one;
        Gizmos.DrawCube(transform.position, size);
    }
#endif
}
