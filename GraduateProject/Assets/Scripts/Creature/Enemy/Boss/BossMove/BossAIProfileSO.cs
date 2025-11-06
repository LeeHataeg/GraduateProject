using UnityEngine;

[CreateAssetMenu(menuName = "Boss/AI Profile (Stage1)")]
public class BossAIProfileSO : ScriptableObject
{
    [Header("Targeting")]
    public LayerMask groundMask;
    public float senseInterval = 0.1f;
    public float losRayLength = 50f;

    [Header("Jump Logic")]
    [Tooltip("플레이어와 y축 차이가 이 값 이상일 때만 점프 고려")]
    public float jumpYThreshold = 3f;
    [Tooltip("점프 시도 최소 간격")]
    public float jumpCooldown = 2.0f;
    [Tooltip("점프 실행을 허용할 최소 수평거리 (너무 붙어있을 땐 점프 X)")]
    public float jumpMinX = 1.5f;
    [Tooltip("점프 실행을 허용할 최대 수평거리 (너무 멀면 점프 X)")]
    public float jumpMaxX = 8f;

    [Header("LightAtk (가벼운 근접)")]
    public float lightMinX = 0.5f;
    public float lightMaxX = 2.6f;
    public float lightMaxAbsY = 1.0f;
    public float lightCooldown = 0.9f;

    [Header("FrontHeavyAtk (전방 대검/돌진 등)")]
    public float heavyMinX = 2.0f;
    public float heavyMaxX = 5.5f;
    public float heavyMaxAbsY = 1.2f;
    public float heavyCooldown = 2.2f;

    [Header("Weights (둘 다 가능할 때 우선순위)")]
    [Range(0f, 1f)] public float preferHeavy = 0.45f;

    [Header("Movement (선택)")]
    public float faceFlipThreshold = 0.1f;
}
