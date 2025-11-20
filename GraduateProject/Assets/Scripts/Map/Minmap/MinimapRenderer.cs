using System.Drawing;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 현재 플레이어가 있는 Room 하나만 미니맵 카메라에 딱 맞게 보여주도록
/// 카메라 위치 / orthographicSize를 조절하는 스크립트.
/// </summary>
[RequireComponent(typeof(Camera))]
public class MinimapRenderer : MonoBehaviour
{
    public static MinimapRenderer Instance { get; private set; }

    [Header("Target Camera (비우면 자기 자신의 Camera 사용)")]
    [SerializeField] private Camera minimapCamera;

    [Header("방 Bounds 대비 여유 비율")]
    [Tooltip("0.1 이면 방 크기에서 세로/가로 10% 정도 여유를 더 줌")]
    [Range(0f, 0.5f)]
    [SerializeField] private float paddingRatio = 0.1f;

    [Header("디버그 로그")]
    [SerializeField] private bool logDebug = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (minimapCamera == null)
            minimapCamera = GetComponent<Camera>();

        if (minimapCamera != null)
        {
            minimapCamera.orthographic = true;
            this.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Room 쪽에서 호출: "플레이어가 이 방에 들어옴"
    /// </summary>


    public static void NotifyPlayerEnteredRoom(Room room)
    {
        if (Instance == null)
        {
            Debug.Log("[Minimap]-인스턴스 없어요");
            return;
        }
        if (room == null)
        {
            Debug.Log("[Minimap]-방을 안줬잖아 씹새야.");
            return;
        }

        Instance.FocusOnRoom(room);
    }

    /// <summary>
    /// 주어진 Room의 타일맵 영역을 계산해서 카메라를 해당 Room에 맞게 세팅
    /// </summary>
    public void FocusOnRoom(Room room)
    {
        if (minimapCamera == null)
        {
            Debug.Log("[Minimap]-카메라 없어요");
            return;
        }

        if (room == null)
        {
            Debug.Log("[Minimap]-방을 안줬잖아 씹새야.");
            return;
        }

        // 1) Room 안의 타일맵들 기준으로 월드 Bounds 계산
        if (!TryGetRoomBounds(room, out Bounds bounds))
        {
            // 타일맵을 못 찾으면 RoomSpace + GetSpawnPosition으로 대충 추정
            Vector2 center = room.GetSpawnPosition();
            Vector3 size = new Vector3(room.RoomSpace.width, room.RoomSpace.height, 0f);
            bounds = new Bounds(center, size);
        }

        // 2) 카메라 위치 = 방 중심
        Vector3 camPos = minimapCamera.transform.position;
        camPos.x = bounds.center.x;
        camPos.y = bounds.center.y;
        minimapCamera.transform.position = camPos;

            // 3) 카메라 orthographicSize 계산
            float halfWidth = bounds.extents.x;
        float halfHeight = bounds.extents.y;

        float aspect = minimapCamera.aspect <= 0f ? 1f : minimapCamera.aspect;

        float sizeByHeight = halfHeight;
        float sizeByWidth = halfWidth / aspect;

        float targetSize = Mathf.Max(sizeByHeight, sizeByWidth);
        targetSize *= (1f + paddingRatio); // 여유 패딩

        minimapCamera.orthographicSize = Mathf.Max(0.1f, targetSize);

        if (logDebug)
        {
            Debug.Log($"[MinimapRenderer] FocusOnRoom '{room.name}' " +
                      $"Center={bounds.center}, Extents={bounds.extents}, " +
                      $"CamSize={minimapCamera.orthographicSize}");
        }
    }

    /// <summary>
    /// Room 안의 Tilemap들에서 월드 Bounds 계산
    /// 우선 태그 Ground/Wall 을 가진 타일맵 기준으로 시도하고,
    /// 없으면 모든 타일맵 합산.
    /// </summary>
    private bool TryGetRoomBounds(Room room, out Bounds result)
    {
        result = new Bounds();
        bool hasBounds = false;

        var tilemaps = room.GetComponentsInChildren<Tilemap>(true);
        if (tilemaps == null || tilemaps.Length == 0)
            return false;

        // 1차: Ground / Wall 태그만
        hasBounds = CollectTilemapBounds(tilemaps, onlyGroundAndWall: true, ref result);

        // 하나도 못 모았으면 → 2차: 모든 타일맵
        if (!hasBounds)
            hasBounds = CollectTilemapBounds(tilemaps, onlyGroundAndWall: false, ref result);

        return hasBounds;
    }

    private bool CollectTilemapBounds(Tilemap[] tilemaps, bool onlyGroundAndWall, ref Bounds acc)
    {
        bool hasBounds = false;

        foreach (var tm in tilemaps)
        {
            if (tm == null) continue;

            GameObject go = tm.gameObject;

            if (onlyGroundAndWall)
            {
                // Ground / Wall 태그가 붙은 타일맵만
                if (!go.CompareTag("Ground") && !go.CompareTag("Wall"))
                    continue;
            }

            // 실제 타일이 있는 영역으로 Bounds 압축
            tm.CompressBounds();
            Bounds lb = tm.localBounds;

            // 로컬 -> 월드
            Vector3 worldMin = tm.transform.TransformPoint(lb.min);
            Vector3 worldMax = tm.transform.TransformPoint(lb.max);

            Bounds wb = new Bounds();
            wb.SetMinMax(Vector3.Min(worldMin, worldMax), Vector3.Max(worldMin, worldMax));

            if (!hasBounds)
            {
                acc = wb;
                hasBounds = true;
            }
            else
            {
                acc.Encapsulate(wb.min);
                acc.Encapsulate(wb.max);
            }
        }

        return hasBounds;
    }
}
