using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>스페이스바 연타로 작살 포획 여부를 결정하는 게이지입니다.</summary>
[DisallowMultipleComponent]
public sealed class HarpoonStruggleGauge : MonoBehaviour
{
    [SerializeField] 
    private Canvas targetCanvas;
    [SerializeField] 
    private Image fillImage;
    [SerializeField, Min(0.1f)] 
    private float duration = 3f;
    [SerializeField, Range(0.01f, 1f)] 
    private float fillPerPress = 0.12f;
    [Tooltip("물고기의 체력이 0에 가까울 때 클릭 증가량에 적용할 배율입니다.")]
    [SerializeField, Min(0.01f)]
    private float lowHealthMultiplier = 1.5f;
    [Tooltip("물고기의 체력이 최대일 때 클릭 증가량에 적용할 배율입니다.")]
    [SerializeField, Min(0.01f)]
    private float highHealthMultiplier = 0.5f;
    [SerializeField, Range(0.01f, 1f)] 
    private float successAmount = 0.75f;
    [SerializeField, Min(0f)] 
    private float decreasePerSecond = 0.05f;

    private Action<bool> completed;
    private float remainingTime;
    private float amount;
    private float currentFillPerPress;
    private bool isPlaying;
    private Vector3 defaultScale;

    private void Awake()
    {
        if (targetCanvas == null) targetCanvas = GetComponent<Canvas>();
        defaultScale = transform.localScale;
        SetVisible(false);
        SetAmount(0f);
    }

    public void Begin(
        Vector2 attackDirection,
        int currentHealth,
        int maxHealth,
        Action<bool> onCompleted)
    {
        Cancel();
        SetDirection(attackDirection);
        float healthRatio = maxHealth > 0
            ? Mathf.Clamp01((float)currentHealth / maxHealth)
            : 0f;
        float healthMultiplier = Mathf.Lerp(
            lowHealthMultiplier,
            highHealthMultiplier,
            healthRatio
        );
        currentFillPerPress = fillPerPress * healthMultiplier;
        completed = onCompleted;
        remainingTime = duration;
        isPlaying = true;
        SetAmount(0f);
        SetVisible(true);
    }

    public void Cancel()
    {
        isPlaying = false;
        completed = null;
        SetVisible(false);
        SetAmount(0f);
    }

    private void Update()
    {
        if (!isPlaying) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            SetAmount(amount + currentFillPerPress);

        SetAmount(amount - decreasePerSecond * Time.unscaledDeltaTime);
        remainingTime -= Time.unscaledDeltaTime;
        if (remainingTime <= 0f) Finish(amount >= successAmount);
    }

    private void Finish(bool succeeded)
    {
        Action<bool> callback = completed;
        isPlaying = false;
        completed = null;
        SetVisible(false);
        callback?.Invoke(succeeded);
    }

    private void SetAmount(float value)
    {
        amount = Mathf.Clamp01(value);
        if (fillImage != null) fillImage.fillAmount = amount;
    }

    private void SetVisible(bool visible)
    {
        if (targetCanvas != null) targetCanvas.enabled = visible;
    }

    private void SetDirection(Vector2 attackDirection)
    {
        // UI 자식들이 기본적으로 플레이어 왼쪽에 있으므로 왼쪽 공격일 때 전체를 반전합니다.
        float directionSign = attackDirection.x >= 0f ? 1f : -1f;
        transform.localScale = new Vector3(
            Mathf.Abs(defaultScale.x) * directionSign,
            defaultScale.y,
            defaultScale.z
        );
    }

    private void OnDisable()
    {
        isPlaying = false;
        completed = null;
    }
}
