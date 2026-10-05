using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>스페이스바를 유지해 플레이어의 공기를 충전하는 산소통입니다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
public sealed class OxygenTank : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private SpriteRenderer tankRenderer;
    [SerializeField] private Sprite openedSprite;
    [SerializeField] private Canvas interactionCanvas;
    [SerializeField] private Image holdGauge;

    [Header("상호작용")]
    [SerializeField, Min(0.1f)] private float holdDuration = 2f;

    private PlayerDiveStatus nearbyPlayer;
    private float holdProgress;
    private bool opened;

    private void Awake()
    {
        if (tankRenderer == null) tankRenderer = GetComponent<SpriteRenderer>();
        SetProgress(0f);
        SetCanvasVisible(false);
    }

    private void Update()
    {
        if (opened || nearbyPlayer == null) return;

        Keyboard keyboard = Keyboard.current;
        bool holding = keyboard != null && keyboard.spaceKey.isPressed;
        if (!holding)
        {
            SetProgress(0f);
            return;
        }

        SetProgress(holdProgress + Time.deltaTime / Mathf.Max(0.1f, holdDuration));
        if (holdProgress >= 1f) CompleteInteraction();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (opened) return;
        PlayerDiveStatus player = other.GetComponentInParent<PlayerDiveStatus>();
        if (player == null) return;

        nearbyPlayer = player;
        SetProgress(0f);
        SetCanvasVisible(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerDiveStatus player = other.GetComponentInParent<PlayerDiveStatus>();
        if (player == null || player != nearbyPlayer) return;

        nearbyPlayer = null;
        SetProgress(0f);
        SetCanvasVisible(false);
    }

    private void CompleteInteraction()
    {
        opened = true;
        nearbyPlayer.RefillAir();
        if (tankRenderer != null && openedSprite != null)
            tankRenderer.sprite = openedSprite;

        nearbyPlayer = null;
        SetProgress(1f);
        SetCanvasVisible(false);
    }

    private void SetProgress(float value)
    {
        holdProgress = Mathf.Clamp01(value);
        if (holdGauge != null) holdGauge.fillAmount = holdProgress;
    }

    private void SetCanvasVisible(bool visible)
    {
        if (interactionCanvas != null) interactionCanvas.enabled = visible;
    }

    private void OnDisable()
    {
        nearbyPlayer = null;
        if (!opened) SetProgress(0f);
        SetCanvasVisible(false);
    }
}
