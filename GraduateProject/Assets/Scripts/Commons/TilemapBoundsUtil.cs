// TilemapBoundsUtil.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class TilemapBoundsUtil
{
    public static bool TryGetCompositeCellBounds(Transform root, HashSet<string> filterNames, out BoundsInt composite, out Vector3Int minCell, out Vector2Int size)
    {
        composite = new BoundsInt();
        minCell = Vector3Int.zero;
        size = Vector2Int.zero;
        if (!root) return false;

        var maps = root.GetComponentsInChildren<Tilemap>(true);
        if (maps == null || maps.Length == 0) return false;

        bool first = true;
        int xMin = 0, yMin = 0, xMax = 0, yMax = 0;

        foreach (var tm in maps)
        {
            if (!tm) continue;
            if (filterNames != null && filterNames.Count > 0)
            {
                var n = tm.gameObject.name;
                if (!filterNames.Contains(n)) continue;
            }

            tm.CompressBounds();
            var b = tm.cellBounds;
            if (first)
            {
                xMin = b.xMin; yMin = b.yMin; xMax = b.xMax; yMax = b.yMax;
                first = false;
            }
            else
            {
                if (b.xMin < xMin) xMin = b.xMin;
                if (b.yMin < yMin) yMin = b.yMin;
                if (b.xMax > xMax) xMax = b.xMax;
                if (b.yMax > yMax) yMax = b.yMax;
            }
        }

        if (first) return false;

        minCell = new Vector3Int(xMin, yMin, 0);
        size = new Vector2Int(xMax - xMin, yMax - yMin);
        composite = new BoundsInt(minCell, new Vector3Int(size.x, size.y, 1));
        return true;
    }
}
