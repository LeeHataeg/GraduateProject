using UnityEngine;

public class PlayerPositionController : MonoBehaviour
{
    private Transform _target;

    public void SetTarget(Transform t) => _target = t;

    public void SetPosition(Vector2 worldPos)
    {
        if (_target != null)
            _target.position = worldPos;
        else
            Debug.LogWarning("[PlayerPositionController] Target 미지정. SetTarget 먼저 호출하세요.");
    }
}
