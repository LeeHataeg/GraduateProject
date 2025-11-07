using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class EchoPersistence
{
    const int MaxTapes = 5;
    static readonly string SaveDir = Path.Combine(Application.persistentDataPath, "EchoRunner");
    static readonly string TapePath = Path.Combine(SaveDir, "death_tapes.json");
    static readonly string StashPath = Path.Combine(SaveDir, "stash_items.json");

    [Serializable] class TapeList { public List<EchoTape> tapes = new(); }
    [Serializable] class Stash { public List<string> itemIds = new(); }

    public static List<EchoTape> LoadTapes()
    {
        try
        {
            if (!File.Exists(TapePath)) return new List<EchoTape>();
            var json = File.ReadAllText(TapePath);
            return JsonUtility.FromJson<TapeList>(json)?.tapes ?? new List<EchoTape>();
        }
        catch { return new List<EchoTape>(); }
    }

    public static void SaveTapes(List<EchoTape> list)
    {
        try
        {
            if (!Directory.Exists(SaveDir)) Directory.CreateDirectory(SaveDir);
            var json = JsonUtility.ToJson(new TapeList { tapes = list }, false);
            File.WriteAllText(TapePath, json);
        }
        catch { }
    }

    public static void PushDeathTapeFIFO(EchoTape tape)
    {
        var list = LoadTapes();
        list.Add(tape);
        while (list.Count > MaxTapes) list.RemoveAt(0);
        SaveTapes(list);
    }

    public static List<string> LoadStash()
    {
        try
        {
            if (!File.Exists(StashPath)) return new List<string>();
            var json = File.ReadAllText(StashPath);
            return JsonUtility.FromJson<Stash>(json)?.itemIds ?? new List<string>();
        }
        catch { return new List<string>(); }
    }

    public static void SaveStash(List<string> ids)
    {
        try
        {
            if (!Directory.Exists(SaveDir)) Directory.CreateDirectory(SaveDir);
            var json = JsonUtility.ToJson(new Stash { itemIds = ids }, false);
            File.WriteAllText(StashPath, json);
        }
        catch { }
    }

    // 클리어 시 기존 Echo 삭제
    // TODO : 보상 제공 구현 필요
    public static void HarvestItemsFromAllTapes_ThenClear()
    {
        var tapes = LoadTapes();
        var stash = LoadStash();
        foreach (var t in tapes)
        {
            // 아이템 보상 제공
            foreach (var id in t.usedItemIds) { stash.Add(id); break; }
        }
        SaveStash(stash);
        // new List<EchoTape> 할당 == 이미 있던거 다 삭제
        SaveTapes(new List<EchoTape>());
    }
}
