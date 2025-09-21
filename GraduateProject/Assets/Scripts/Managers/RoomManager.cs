using System;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public event Action<Vector2> OnSetStartPoint;

    private Vector2? _startPoint;
    public bool HasStartPoint => _startPoint.HasValue;

    public void SetStartPoint(Vector2 pos)
    {
        _startPoint = pos;
        Debug.Log($"[RoomManager] SetStartPoint({pos})  (scene={gameObject.scene.name})");
        OnSetStartPoint?.Invoke(pos);
    }

    public Vector2 GetStartPoint() => _startPoint ?? Vector2.zero;

    [ContextMenu("DEBUG: Dump State")]
    public void DebugDump()
    {
        Debug.Log($"[RoomManager.Dump] HasStartPoint={HasStartPoint}  value={(_startPoint.HasValue ? _startPoint.Value.ToString() : "null")}");
    }
}
