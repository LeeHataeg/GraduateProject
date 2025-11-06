using System.Collections.Generic;
using UnityEngine;
using static Define;

public static class EnemyArchetypeRegistry
{
    // stage -> (roomType -> catalog)
    private static readonly Dictionary<int, Dictionary<RoomType, EnemyArchetypeCatalog>> byStage
        = new();

    private static bool loaded = false;

    // 기본 검색 경로들(Resources)
    private static readonly string[] DefaultPaths =
    {
        "Enemies/Archetypes",
        "SO/Stats/Enemies/Archetype",
        "" // 전체 Resources 스캔(폴백)
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
                if (!byStage.TryGetValue(c.stage, out var dict))
                {
                    dict = new Dictionary<RoomType, EnemyArchetypeCatalog>();
                    byStage[c.stage] = dict;
                }
                // 동일 키 충돌 시 마지막으로 로드된 걸로 덮어쓰기(간단한 규칙)
                dict[c.roomType] = c;
                found++;
            }
            if (found > 0) break; // 첫 경로에서 찾았으면 종료(로드 비용 절감)
        }

#if UNITY_EDITOR
        if (found == 0)
            Debug.LogWarning("[EnemyArchetypeRegistry] No catalogs found. " +
                             "Create EnemyArchetypeCatalog under Resources.");
#endif
        loaded = true;
    }

    // ★ 신규 API: stage + roomType으로 조회
    public static EnemyArchetypeCatalog GetCatalog(int stage, RoomType roomType, params string[] preferredPaths)
    {
        if (!loaded) LoadAll(preferredPaths);
        if (byStage.TryGetValue(stage, out var dict))
        {
            dict.TryGetValue(roomType, out var c);
            return c;
        }
        return null;
    }

    // 구버전 호환(스테이지 미지정 시 1로 가정)
    public static EnemyArchetypeCatalog GetCatalog(RoomType roomType, params string[] preferredPaths)
        => GetCatalog(1, roomType, preferredPaths);
}
