using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum SushiShiftState
{
    Preparing,
    Running,
    Ending
}

public sealed class SushiShiftController : MonoBehaviour
{
    [Header("Start")]
    [SerializeField] private GameObject startPanel;
    [SerializeField] private Image startGauge;
    [SerializeField, Min(0.1f)] private float startHoldDuration = 1.5f;

    [Header("Shift Timer")]
    [SerializeField] private Image timerGauge;
    [SerializeField, Min(1f)] private float shiftDuration = 90f;
    [SerializeField, Range(0f, 1f)] private float timerEndFill = 0.6f;

    [Header("Systems")]
    [SerializeField] private SushiPlayerController playerController;
    [SerializeField] private CustomerSpawner customerSpawner;

    [Header("HUD")]
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private TMP_Text dateText;

    [Header("Close Result")]
    [SerializeField] private GameObject closePanel;
    [SerializeField] private TMP_Text servedCountText;
    [SerializeField] private TMP_Text dailyRevenueText;
    [SerializeField] private Button exitButton;

    private float startHoldTime;
    private float shiftElapsed;
    private int servedCustomerCount;
    private int dailyRevenue;
    private bool isLeavingScene;

    public SushiShiftState State { get; private set; } = SushiShiftState.Preparing;

    private void Start()
    {
        State = SushiShiftState.Preparing;
        startHoldTime = 0f;
        shiftElapsed = 0f;
        servedCustomerCount = 0;
        dailyRevenue = 0;
        isLeavingScene = false;

        if (startPanel != null)
            startPanel.SetActive(true);
        if (closePanel != null)
            closePanel.SetActive(false);
        if (startGauge != null)
            startGauge.fillAmount = 0f;
        if (timerGauge != null)
            timerGauge.fillAmount = 1f;
        if (playerController != null)
            playerController.SetControlEnabled(false);

        if (GameSession.HasInstance)
            GameSession.Instance.StateChanged += RefreshHud;
        if (exitButton != null)
            exitButton.onClick.AddListener(ExitToNextDay);

        RefreshHud();
    }

    private void Update()
    {
        if (State == SushiShiftState.Preparing)
            UpdateStartInput();
        else if (State == SushiShiftState.Running)
            UpdateShiftTimer();
    }

    private void UpdateStartInput()
    {
        Keyboard keyboard = Keyboard.current;
        bool holding = keyboard != null && keyboard.eKey.isPressed;

        startHoldTime = holding ? startHoldTime + Time.deltaTime : 0f;
        float progress = Mathf.Clamp01(startHoldTime / startHoldDuration);

        if (startGauge != null)
            startGauge.fillAmount = progress;
        if (progress >= 1f)
            BeginShift();
    }

    private void BeginShift()
    {
        State = SushiShiftState.Running;
        shiftElapsed = 0f;
        servedCustomerCount = 0;
        dailyRevenue = 0;

        if (startPanel != null)
            startPanel.SetActive(false);
        if (timerGauge != null)
            timerGauge.fillAmount = 1f;
        if (playerController != null)
            playerController.SetControlEnabled(true);
        if (customerSpawner != null)
            customerSpawner.BeginSpawning();
    }

    private void UpdateShiftTimer()
    {
        shiftElapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(shiftElapsed / Mathf.Max(1f, shiftDuration));

        if (timerGauge != null)
            timerGauge.fillAmount = Mathf.Lerp(1f, timerEndFill, progress);
        if (progress >= 1f)
            EndShift();
    }

    private void EndShift()
    {
        if (State == SushiShiftState.Ending)
            return;

        State = SushiShiftState.Ending;
        if (timerGauge != null)
            timerGauge.fillAmount = timerEndFill;
        if (customerSpawner != null)
            customerSpawner.CloseStore();
        if (playerController != null)
            playerController.SetControlEnabled(false);

        RefreshCloseResult();
        if (closePanel != null)
            closePanel.SetActive(true);
    }

    public bool TryRecordCompletedSale(int foodPrice)
    {
        if (State != SushiShiftState.Running)
            return false;

        servedCustomerCount++;
        dailyRevenue += Mathf.Max(0, foodPrice);
        return true;
    }

    private void RefreshCloseResult()
    {
        if (servedCountText != null)
            servedCountText.text = servedCustomerCount.ToString();
        if (dailyRevenueText != null)
            dailyRevenueText.text = dailyRevenue.ToString();
    }

    private void ExitToNextDay()
    {
        if (State != SushiShiftState.Ending || isLeavingScene)
            return;

        isLeavingScene = true;
        if (exitButton != null)
            exitButton.interactable = false;

        if (GameFlow.CompleteSushiShift(0))
            return;

        isLeavingScene = false;
        if (exitButton != null)
            exitButton.interactable = true;
        Debug.LogError("스시 영업 종료와 다음 날 이동에 실패했습니다.", this);
    }

    private void RefreshHud()
    {
        if (!GameSession.HasInstance)
            return;

        if (coinText != null)
            coinText.text = GameSession.Instance.Money.ToString();
        if (dateText != null)
            dateText.text = $"Day {GameSession.Instance.Day}";
    }

    private void OnDestroy()
    {
        if (exitButton != null)
            exitButton.onClick.RemoveListener(ExitToNextDay);
        if (GameSession.HasInstance)
            GameSession.Instance.StateChanged -= RefreshHud;
    }
}
