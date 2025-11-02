using UnityEngine;

[DisallowMultipleComponent]
public sealed class DestroySentinel : MonoBehaviour
{
    private void OnDestroy()
    {
        var tag = GetComponent<DestroyTraceTag>();
        var path = GetHierarchyPath(transform);
        if (tag != null)
        {
            Debug.Log($"[SENTINEL] Destroyed GO='{name}' scene={gameObject.scene.name} path={path}\n" +
                      $"  killer={tag.killer} reason='{tag.reason}' recordedScene={tag.scene} atFrame={tag.atFrame}\n" +
                      $"  stack:\n{tag.stack}");
        }
        else
        {
            Debug.Log($"[SENTINEL] Destroyed GO='{name}' scene={gameObject.scene.name} path={path}\n" +
                      $"  killer=<unknown> (씬 언로드/에디터 수동 삭제/패치 미적용 등)");
        }
    }

    public static string GetHierarchyPath(Transform t)
    {
        if (t == null) return "<null>";
        System.Text.StringBuilder sb = new System.Text.StringBuilder(64);
        while (t != null) { sb.Insert(0, "/" + t.name); t = t.parent; }
        return sb.ToString();
    }
}
