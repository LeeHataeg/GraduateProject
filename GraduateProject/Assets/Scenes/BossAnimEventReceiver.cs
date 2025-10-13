using UnityEngine;
using System.Collections;

public class BossAnimEventReceiver : MonoBehaviour
{
    [Header("Optional (비워도 됨)")]
    [SerializeField] private Camera targetCam; // Inspector에서 Player 하위 카메라를 드래그해도 OK



    // AnimationEvent: Taunt 클립에서 호출됨
    public void OnShakeCam()
    {
        OnShakeCamLight();
    }

    public void OnShakeCamLight()
    {
        if (!EnsureCamera())
            return; // 카메라 아직 못 찾으면 그냥 무시(또 호출되면 그때 시도)

        // 간단 쉐이크 (원하면 Cinemachine으로 교체)
        StartCoroutine(Shake(0.12f, 0.15f));
    }

    // BossAnimEventReceiver.cs

    public void OnShakeCamHeavy()      // 파라미터 없는 이벤트라면 이 시그니처여야 함
    {
        if (!EnsureCamera()) return;
        // 라이트보다 강하고 길게
        StartCoroutine(Shake(0.25f, 0.30f));
    }

    // 만약 클립 이벤트에 float 인자를 넣어둔 경우(예: 0.35f),
    // 아래처럼 float 파라미터 버전도 지원 가능. (둘 중 하나만 쓰면 됨)
    public void OnShakeCamHeavy(float strength)
    {
        if (!EnsureCamera()) return;
        // strength를 진폭/시간에 반영 (원하는 대로 매핑)
        float amplitude = Mathf.Clamp01(strength) * 0.35f;
        float duration = 0.20f + Mathf.Clamp01(strength) * 0.20f;
        StartCoroutine(Shake(amplitude, duration));
    }


    private bool EnsureCamera()
    {
        if (targetCam != null) return true;

        // 1) 가장 먼저 MainCamera 태그 시도 (플레이어 하위여도 MainCamera면 잡힘)
        var cam = Camera.main;
        if (cam == null)
        {
            // 2) 그래도 없으면 Player 태그로 찾아서 자식에서 Camera 검색
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                cam = player.GetComponentInChildren<Camera>(true);
        }

        // 3) 최후의 수단: 씬의 모든 Camera 중 enabled && gameObject.activeInHierarchy
        if (cam == null)
        {
            var all = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var c in all)
            {
                if (c != null && c.isActiveAndEnabled) { cam = c; break; }
            }
        }

        targetCam = cam;
        return targetCam != null;
    }

    private IEnumerator Shake(float duration, float magnitude)
    {
        // 카메라가 Player 하위여도 localPosition 기준으로 흔들면 원래 위치 복원 쉬움
        Transform t = targetCam.transform;
        Vector3 origin = t.localPosition;

        float tSum = 0f;
        while (tSum < duration && targetCam != null)
        {
            t.localPosition = origin + new Vector3(
                (Random.value * 2f - 1f) * magnitude,
                (Random.value * 2f - 1f) * magnitude,
                0f);
            tSum += Time.unscaledDeltaTime;
            yield return null;
        }
        if (targetCam != null) t.localPosition = origin;
    }

    public void OnResetVelocity()
    {
        var rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = GetComponentInParent<Rigidbody2D>();
        if (rb == null) return;

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector2.zero;   // Unity 6 (Rigidbody2D 신 API)
#else
        rb.velocity = Vector2.zero;         // Unity 2022/2021 등 구버전
#endif
        rb.angularVelocity = 0f;

    }

    // 만약 이벤트가 float 파라미터를 넘긴다면(클립의 Events 창에서 숫자 넣었음)
    public void OnResetVelocity(float dampFactor)
    {
        var rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = GetComponentInParent<Rigidbody2D>();
        if (rb == null) return;

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity *= dampFactor;
#else
        rb.velocity *= dampFactor;
#endif
        rb.angularVelocity = 0f;
    }
}
