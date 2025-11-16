using TMPro;
using UnityEngine;
using static Define;

public class MagCountUI : MonoBehaviour
{
    [SerializeField] private GameObject magPanel;
    [SerializeField] private TextMeshProUGUI magCount;

    private int curMagCount;
    private int maxMagCount;

    public void SetActivation(bool on)
    {
        magPanel.SetActive(on);
    }

    public void SetRanged(int maxMagCount, int curMagCount)
    {
        this.maxMagCount = maxMagCount;
        this.curMagCount = curMagCount;

        magCount.text = this.curMagCount.ToString() + " / " + this.maxMagCount.ToString();
    }

    public void SetCurMagCount(int curMagCount)
    {
        this.curMagCount = curMagCount;
        magCount.text = this.curMagCount.ToString() + " / " + maxMagCount.ToString();
    }
}