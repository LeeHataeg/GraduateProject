// BossFieldAutoAlign.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[DefaultExecutionOrder(-10)]
public class BossFieldAutoAlign : MonoBehaviour
{
    public string[] includeTilemapNames;   // ex) {"Ground","Wall"} 비워두면 모든 Tilemap 포함
    public bool alignOnAwake = true;

    [ContextMenu("Align Now")]
    public void Align()
    {
        HashSet<string> filter = null;
        if (includeTilemapNames != null && includeTilemapNames.Length > 0)
            filter = new HashSet<string>(includeTilemapNames);

        if (!TilemapBoundsUtil.TryGetCompositeCellBounds(transform, filter, out var _, out var _, out var size))
        {
            Debug.LogWarning("[BossFieldAutoAlign] No Tilemap found.");
            return;
        }

        transform.position = new Vector3(-size.x, -size.y, 0f);
#if UNITY_EDITOR
        Debug.Log($"[BossFieldAutoAlign] placed at (-{size.x}, -{size.y}, 0)");
#endif
    }

    private void Awake()
    {
        if (alignOnAwake) Align();
    }
}
