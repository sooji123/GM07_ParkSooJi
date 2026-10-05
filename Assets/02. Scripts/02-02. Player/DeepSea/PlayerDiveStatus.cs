using System.Collections;
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

    [Header("피격 연출")]
    [SerializeField] private Image damageImage;
    [SerializeField] private SpriteRenderer playerRenderer;
    [SerializeField] private CameraDirector cameraDirector;
    [SerializeField, Min(0f)] private float invincibilityDuration = 1.5f;
    [SerializeField, Min(0.02f)] private float blinkInterval = 0.12f;
    [Tooltip("피해 이미지가 투명도 0에서 1로 올라갔다가 다시 0으로 돌아오는 전체 시간입니다.")]
    [SerializeField, Min(0.01f)] private float damageImageFadeDuration = 0.5f;
    [SerializeField] private Color damagedAirTextColor = Color.red;
    [SerializeField, Min(0f)] private float damagedAirTextDuration = 0.5f;
    [SerializeField, Min(0f)] private float damageShakeDuration = 0.25f;
    [SerializeField, Min(0f)] private float damageShakeStrength = 0.06f;

    public float CurrentAir { get; private set; }
    public float CurrentDepth { get; private set; }
    public float MaxDepthReached { get; private set; }
    public bool IsInvincible => Time.time < invincibilityEndTime;
    private PlayerMotor motor;
    private PlayerController controller;
    private float invincibilityEndTime;
    private Coroutine damageRoutine;
    private Color damageImageColor;
    private Color normalAirTextColor;

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        controller = GetComponent<PlayerController>();
        if (playerRenderer == null) playerRenderer = GetComponentInChildren<SpriteRenderer>(true);
        if (cameraDirector == null && Camera.main != null)
            cameraDirector = Camera.main.GetComponent<CameraDirector>();
        if (damageImage != null)
        {
            damageImageColor = damageImage.color;
            SetDamageImageAlpha(0f);
        }
        if (airText != null) normalAirTextColor = airText.color;
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

    /// <summary>플레이어의 공기를 최대치까지 채웁니다.</summary>
    public void RefillAir()
    {
        CurrentAir = Mathf.Max(0.01f, maxAir);
        RefreshUI();
    }

    /// <summary>공격이나 환경 피해만큼 현재 공기를 감소시킵니다.</summary>
    public void ReduceAir(float amount)
    {
        CurrentAir = Mathf.Max(0f, CurrentAir - Mathf.Max(0f, amount));
        RefreshUI();
    }

    /// <summary>무적 상태가 아닐 때 피해와 피격 연출을 적용합니다.</summary>
    public bool TryTakeDamage(float amount)
    {
        if (amount <= 0f || IsInvincible) return false;

        ReduceAir(amount);
        invincibilityEndTime = Time.time + Mathf.Max(0f, invincibilityDuration);
        if (cameraDirector != null)
            cameraDirector.Shake(damageShakeDuration, damageShakeStrength);

        if (damageRoutine != null) StopCoroutine(damageRoutine);
        damageRoutine = StartCoroutine(PlayDamageFeedback());
        return true;
    }

    private IEnumerator PlayDamageFeedback()
    {
        float invincibleDuration = Mathf.Max(0f, invincibilityDuration);
        float imageDuration = Mathf.Max(0.01f, damageImageFadeDuration);
        float airTextDuration = Mathf.Max(0f, damagedAirTextDuration);
        float duration = Mathf.Max(invincibleDuration, imageDuration, airTextDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (playerRenderer != null)
            {
                if (elapsed < invincibleDuration)
                {
                    int blinkStep = Mathf.FloorToInt(elapsed / Mathf.Max(0.02f, blinkInterval));
                    playerRenderer.enabled = blinkStep % 2 == 0;
                }
                else
                {
                    playerRenderer.enabled = true;
                }
            }

            float imageProgress = Mathf.Clamp01(elapsed / imageDuration);
            float imageAlpha = 1f - Mathf.Abs(imageProgress * 2f - 1f);
            SetDamageImageAlpha(imageAlpha);

            if (airText != null)
                airText.color = elapsed < airTextDuration
                    ? damagedAirTextColor
                    : normalAirTextColor;
            yield return null;
        }

        ResetDamageFeedback();
        damageRoutine = null;
    }

    private void SetDamageImageAlpha(float alpha)
    {
        if (damageImage == null) return;
        Color color = damageImageColor;
        color.a = Mathf.Clamp01(alpha);
        damageImage.color = color;
    }

    private void ResetDamageFeedback()
    {
        if (playerRenderer != null) playerRenderer.enabled = true;
        if (airText != null) airText.color = normalAirTextColor;
        SetDamageImageAlpha(0f);
    }

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

    private void OnDisable()
    {
        if (damageRoutine != null)
        {
            StopCoroutine(damageRoutine);
            damageRoutine = null;
        }
        invincibilityEndTime = 0f;
        ResetDamageFeedback();
    }
}
