using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BoatScenePresenter : MonoBehaviour
{
    private static readonly DateTime StartDate = new DateTime(2023, 10, 1);

    private bool isSubscribed;

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
        SubscribeToSession();
        Refresh();
    }

    private void OnEnable()
    {
        SubscribeToSession();
    }

    private void OnDisable()
    {
        if (isSubscribed && GameSession.HasInstance)
            GameSession.Instance.StateChanged -= Refresh;
        isSubscribed = false;
    }

    private void SubscribeToSession()
    {
        if (isSubscribed || !GameSession.HasInstance)
            return;

        GameSession.Instance.StateChanged += Refresh;
        isSubscribed = true;
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
        DateTime currentDate = StartDate.AddDays(Mathf.Max(1, session.Day) - 1);
        dateText.text = currentDate.ToString("ddd MM. dd", CultureInfo.InvariantCulture);
        coinText.text = session.Money.ToString();
    }
}
