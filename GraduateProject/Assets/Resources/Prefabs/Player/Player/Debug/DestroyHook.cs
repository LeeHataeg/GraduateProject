#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class DestroyHook
{
    private static readonly Harmony _harmony;

    static DestroyHook()
    {
        try
        {
            _harmony = new Harmony("com.yourgame.destroyhook");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
            UnityEngine.Debug.Log("[DestroyHook] Harmony patches installed.");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[DestroyHook] Patch failed: {ex}");
        }
    }

    // 공통: 대상에서 GameObject를 뽑는다.
    private static GameObject AsGO(UnityEngine.Object obj)
    {
        if (obj is GameObject go) return go;
        if (obj is Component c) return c.gameObject;
        return null;
    }

    // 호출자 문자열 생성(파일::함수:라인)
    private static string CallerString()
    {
        var st = new StackTrace(2, true); // 0=이 함수, 1=Prefix, 2=실제 호출자 프레임부터
        for (int i = 0; i < st.FrameCount; i++)
        {
            var f = st.GetFrame(i);
            var m = f.GetMethod();
            if (m == null) continue;
            var declType = m.DeclaringType;
            // UnityEngine 내부/Editor 내부 프레임은 스킵
            if (declType == null) continue;
            var ns = declType.Namespace ?? "";
            if (ns.StartsWith("UnityEngine")) continue;
            if (ns.StartsWith("UnityEditor")) continue;

            string file = f.GetFileName();
            string fileShort = string.IsNullOrEmpty(file) ? "<nofile>" : Path.GetFileName(file);
            return $"{fileShort}::{m.Name}:{f.GetFileLineNumber()}";
        }
        return "<unknown>";
    }

    private static string FullStack()
    {
        // Unity 형식의 스택 문자열
        return StackTraceUtility.ExtractStackTrace();
    }

    private static void Tag(GameObject go, string reason = null)
    {
        if (!go) return;
        var tag = go.GetComponent<DestroyTraceTag>();
        if (!tag) tag = go.AddComponent<DestroyTraceTag>();

        tag.killer = CallerString();
        tag.reason = reason ?? "";
        tag.scene = SceneManager.GetActiveScene().name;
        tag.atFrame = Time.frameCount;
        tag.stack = FullStack();
    }

    // ---------- PATCHES ----------

    [HarmonyPatch(typeof(UnityEngine.Object), nameof(UnityEngine.Object.Destroy), new Type[] { typeof(UnityEngine.Object) })]
    class Patch_Destroy_1
    {
        static void Prefix(UnityEngine.Object obj)
        {
            var go = DestroyHook.AsGO(obj);
            if (go) DestroyHook.Tag(go);
        }
    }

    [HarmonyPatch(typeof(UnityEngine.Object), nameof(UnityEngine.Object.Destroy), new Type[] { typeof(UnityEngine.Object), typeof(float) })]
    class Patch_Destroy_2
    {
        static void Prefix(UnityEngine.Object obj, float t)
        {
            var go = DestroyHook.AsGO(obj);
            if (go) DestroyHook.Tag(go, reason: $"delay={t}");
        }
    }

    [HarmonyPatch(typeof(UnityEngine.Object), nameof(UnityEngine.Object.DestroyImmediate), new Type[] { typeof(UnityEngine.Object), typeof(bool) })]
    class Patch_DestroyImmediate_1
    {
        static void Prefix(UnityEngine.Object obj, bool allowDestroyingAssets)
        {
            var go = DestroyHook.AsGO(obj);
            if (go) DestroyHook.Tag(go, reason: $"immediate assets={allowDestroyingAssets}");
        }
    }

    [HarmonyPatch(typeof(UnityEngine.Object), nameof(UnityEngine.Object.DestroyImmediate), new Type[] { typeof(UnityEngine.Object) })]
    class Patch_DestroyImmediate_2
    {
        static void Prefix(UnityEngine.Object obj)
        {
            var go = DestroyHook.AsGO(obj);
            if (go) DestroyHook.Tag(go, reason: "immediate");
        }
    }
}
#endif
