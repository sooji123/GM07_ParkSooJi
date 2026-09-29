using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>공기 소모와 현재·최대 수심을 기록하고 잠수 UI를 갱신합니다.</summary>
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(PlayerMotor))]
public sealed class PlayerDiveStatus : MonoBehaviour
{
    [Header("공기")]
    [SerializeField, Min(0.01f)] private float maxAir = 100f;
    [SerializeField, Min(0f)] private float airPerSecond = 0.2f;
    [SerializeField, Min(0f)] private float sprintAirPerSecond = 0.8f;
    [Header("수심")]
    [SerializeField] private Transform waterSurface;
    [SerializeField] private float surfaceWorldY;
    [SerializeField] private Transform depthTarget;
    [SerializeField, Min(0.01f)] private float metersPerUnit = 1f;
    [Header("UI 참조")]
    [SerializeField] private Image airGauge;
    [SerializeField] private TMP_Text airText;
    [SerializeField] private TMP_Text depthText;

    public float CurrentAir { get; private set; }
    public float CurrentDepth { get; private set; }
    public float MaxDepthReached { get; private set; }
    private PlayerMotor motor;
    private PlayerController controller;

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        controller = GetComponent<PlayerController>();
        CurrentAir = Mathf.Max(0.01f, maxAir);
        RefreshUI();
    }

    private void FixedUpdate()
    {
        // 상태 갱신 뒤 처리하며, 정지·조준·공격 중에는 기본 소모만 적용합니다.
        bool sprinting = motor.IsSprinting && controller != null
            && controller.isActiveAndEnabled && controller.State == PlayerState.Move;
        float rate = Mathf.Max(0f, airPerSecond)
            + (sprinting ? Mathf.Max(0f, sprintAirPerSecond) : 0f);
        CurrentAir = ConsumeAir(CurrentAir, rate, Time.fixedDeltaTime);
    }

    public static float ConsumeAir(float air, float rate, float deltaTime)
        => Mathf.Max(0f, air - Mathf.Max(0f, rate) * Mathf.Max(0f, deltaTime));

    public static float CalculateDepth(float surfaceY, float targetY, float scale)
        => Mathf.Max(0f, surfaceY - targetY) * Mathf.Max(0.01f, scale);

    private void LateUpdate() => RefreshUI();

    private void RefreshUI()
    {
        float surfaceY = waterSurface != null ? waterSurface.position.y : surfaceWorldY;
        float targetY = depthTarget != null ? depthTarget.position.y : transform.position.y;
        CurrentDepth = CalculateDepth(surfaceY, targetY, metersPerUnit);
        MaxDepthReached = Mathf.Max(MaxDepthReached, CurrentDepth);
        if (airGauge != null) airGauge.fillAmount = Mathf.Clamp01(CurrentAir / Mathf.Max(0.01f, maxAir));
        if (airText != null) airText.SetText("{0}", Mathf.CeilToInt(CurrentAir));
        if (depthText != null) depthText.SetText("{0:1}m", CurrentDepth);
    }
}
