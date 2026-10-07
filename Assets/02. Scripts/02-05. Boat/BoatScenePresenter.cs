using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BoatScenePresenter : MonoBehaviour
{
    [Header("Background")]
    [SerializeField]
    private SpriteRenderer backgroundRenderer;

    [SerializeField]
    private Sprite morningBackground;

    [SerializeField]
    private Sprite afternoonBackground;

    [SerializeField]
    private Sprite eveningBackground;

    [Header("Time Gauge")]
    [SerializeField]
    private Image timeGaugeImage;

    [SerializeField]
    private Sprite morningTimeGauge;

    [SerializeField]
    private Sprite afternoonTimeGauge;

    [SerializeField]
    private Sprite eveningTimeGauge;

    [Header("Text")]
    [SerializeField]
    private TMP_Text dateText;

    [SerializeField]
    private TMP_Text coinText;

    private void Start()
    {
        Refresh();
    }
    public void Refresh()
    {
        GameSession session = GameSession.Instance;
        if(session == null)
        {
            Debug.LogError("GameSession instance is null.");
            return;
        }
        switch(session.Phase)
        {
            case EDayPhase.Morning:
                backgroundRenderer.sprite = morningBackground;
                timeGaugeImage.sprite = morningTimeGauge;
                break;
            case EDayPhase.Afternoon:
                backgroundRenderer.sprite = afternoonBackground;
                timeGaugeImage.sprite = afternoonTimeGauge;
                break;
            case EDayPhase.Evening:
                backgroundRenderer.sprite = eveningBackground;
                timeGaugeImage.sprite = eveningTimeGauge;
                break;
            default:
                Debug.LogError("Unknown day phase: " + session.Phase);
                break;
        }
        dateText.text = $"Day {session.Day}";
        coinText.text = session.Money.ToString();
    }
}
