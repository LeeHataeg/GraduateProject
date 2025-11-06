using UnityEngine;

public class BossExitPortal : MonoBehaviour
{
    public GameObject portalVisual;

    void Awake()
    {
        if (portalVisual) portalVisual.SetActive(false);
        gameObject.SetActive(false);
    }

    public void Open()
    {
        if (portalVisual) portalVisual.SetActive(true);
        gameObject.SetActive(true);
    }
}
