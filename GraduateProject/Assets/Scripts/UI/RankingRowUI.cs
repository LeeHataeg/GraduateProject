using TMPro;
using UnityEngine;

public class RankingRowUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI idText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI timeText;

    public void ShowEntry(int rank, string userIdOrName, int score, float timeSec)
    {
        gameObject.SetActive(true);

        if (rankText != null)
            rankText.text = $"{rank}등";

        if (idText != null)
            idText.text = userIdOrName;

        if (scoreText != null)
            scoreText.text = $"{score}점";

        if (timeText != null)
            timeText.text = FormatTime(timeSec);
    }

    public void ShowEllipsis()
    {
        gameObject.SetActive(true);

        if (rankText != null)
            rankText.text = "~";

        if (idText != null)
            idText.text = "~ ~ ~";

        if (scoreText != null)
            scoreText.text = string.Empty;

        if (timeText != null)
            timeText.text = string.Empty;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private string FormatTime(float t)
    {
        int minutes = Mathf.FloorToInt(t / 60f);
        int seconds = Mathf.FloorToInt(t % 60f);
        return $"{minutes}분 {seconds:00}초";
    }
}
