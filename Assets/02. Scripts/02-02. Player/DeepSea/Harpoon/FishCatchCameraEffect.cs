using UnityEngine;

/// <summary>플레이어의 조준과 공격 상태에 맞춰 카메라 연출을 재생합니다.</summary>
[DisallowMultipleComponent]
public sealed class FishCatchCameraEffect : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private PlayerController player;
    [SerializeField] private HarpoonController harpoon;
    [SerializeField] private CameraDirector cameraDirector;

    [Header("조준 연출")]
    [Tooltip("Perspective 카메라는 값이 작을수록 확대됩니다.")]
    [SerializeField, Range(1f, 179f)] private float aimZoom = 48f;
    [SerializeField, Min(0f)] private float zoomInDuration = 0.45f;

    [Header("공격 연출")]
    [SerializeField, Min(0f)] private float shakeStrength = 0.025f;

    [Header("기본 상태 복귀")]
    [SerializeField, Min(0f)] private float zoomResetDuration = 0.3f;

    private void Awake()
    {
        if (player == null) player = GetComponent<PlayerController>();
        if (harpoon == null) harpoon = GetComponent<HarpoonController>();
        if (cameraDirector == null && Camera.main != null)
            cameraDirector = Camera.main.GetComponent<CameraDirector>();
    }

    private void OnEnable()
    {
        if (player == null) return;
        player.StateChanged += OnStateChanged;
        if (harpoon != null) harpoon.FishHit += OnFishHit;
        OnStateChanged(player.State);
    }

    private void OnDisable()
    {
        if (player != null) player.StateChanged -= OnStateChanged;
        if (harpoon != null) harpoon.FishHit -= OnFishHit;
        if (cameraDirector != null)
        {
            cameraDirector.StopShake();
            cameraDirector.ResetZoom(0f);
        }
    }

    private void OnStateChanged(PlayerState state)
    {
        if (cameraDirector == null) return;
        switch (state)
        {
            case PlayerState.Aim:
                cameraDirector.StopShake();
                cameraDirector.ZoomIn(aimZoom, zoomInDuration);
                break;
            case PlayerState.Attack:
                // 공격 직후에는 확대만 유지하고, 물고기에 적중했을 때 흔들림을 시작합니다.
                cameraDirector.StopShake();
                break;
            default:
                cameraDirector.StopShake();
                cameraDirector.ResetZoom(zoomResetDuration);
                break;
        }
    }

    private void OnFishHit(Fish fish)
    {
        if (cameraDirector != null && player != null && player.State == PlayerState.Attack)
            cameraDirector.StartShake(shakeStrength);
    }
}
