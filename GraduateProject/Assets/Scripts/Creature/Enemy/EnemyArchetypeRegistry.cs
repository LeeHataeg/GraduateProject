using System.Collections.Generic;
using UnityEngine;
using static Define;

public static class EnemyArchetypeRegistry
{
    private static readonly Dictionary<RoomType, EnemyArchetypeCatalog> byType = new();
    private static bool loaded = false;

    // 기본 후보 경로들(원본 + 네가 실제 둔 경로 + 전체 스캔)
    private static readonly string[] DefaultPaths =
    {
        "Enemies/Archetypes",
        "SO/Stats/Enemies/Archetype",
        "" // ← 전체 Resources 스캔(비권장이나 폴백)
    };

    public static void LoadAll(params string[] searchPaths)
    {
        if (loaded) return;

        var paths = (searchPaths != null && searchPaths.Length > 0) ? searchPaths : DefaultPaths;
        int found = 0;

        foreach (var p in paths)
        {
            var all = Resources.LoadAll<EnemyArchetypeCatalog>(p);
            foreach (var c in all)
            {
                if (!c) continue;
                byType[c.roomType] = c;
                found++;
            }
            if (found > 0) break; // 첫 경로에서 찾으면 종료
        }

#if UNITY_EDITOR
        if (found == 0)
            Debug.LogWarning("[EnemyArchetypeRegistry] No catalogs found in any Resources path. " +
                             "Create an EnemyArchetypeCatalog under Resources.");
#endif
        loaded = true;
    }

    public static EnemyArchetypeCatalog GetCatalog(RoomType roomType, params string[] preferredPaths)
    {
        if (!loaded) LoadAll(preferredPaths);
        byType.TryGetValue(roomType, out var c);
        return c;
    }
}
