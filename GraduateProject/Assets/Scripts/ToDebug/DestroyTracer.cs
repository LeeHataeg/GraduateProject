using UnityEngine;
using System.Diagnostics;

public static class DestroyTracer
{
    [Conditional("DEBUG")]
    static void Log(object target, string note)
    {
        var st = new StackTrace(true);
        UnityEngine.Debug.Log($"[DESTROY] target={NameOf(target)} note={note}\n{st}");
    }

    public static void Kill(Object target, string note = "")
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Log(target, note);
#endif
        if (target != null) Object.Destroy(target);
    }

    public static void KillImmediate(Object target, string note = "")
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Log(target, "[IMMEDIATE] " + note);
#endif
#if UNITY_EDITOR
        if (target != null) Object.DestroyImmediate(target);
#else
        if (target != null) Object.Destroy(target);
#endif
    }

    static string NameOf(object o) => o is Object u ? $"{u.name} ({u.GetType().Name})" : (o?.ToString() ?? "null");
}
