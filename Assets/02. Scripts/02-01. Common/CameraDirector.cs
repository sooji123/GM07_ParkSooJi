using System.Collections;
using UnityEngine;

/// <summary>외부에서 호출할 수 있는 카메라 줌과 흔들림 기능을 제공합니다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera), typeof(CameraController))]
public sealed class CameraDirector : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private CameraController cameraController;
    [Tooltip("일시정지 중에도 카메라 연출을 재생합니다.")]
    [SerializeField] private bool useUnscaledTime = true;

    private float defaultZoom;
    private Coroutine zoomRoutine;
    private Coroutine shakeRoutine;

    private void Awake()
    {
        if (targetCamera == null) targetCamera = GetComponent<Camera>();
        if (cameraController == null) cameraController = GetComponent<CameraController>();
        if (targetCamera != null)
            defaultZoom = GetCurrentZoom();
    }

    public void ZoomIn(float targetZoom, float duration)
    {
        StartZoom(targetZoom, duration);
    }

    public void ZoomOut(float targetZoom, float duration)
    {
        StartZoom(targetZoom, duration);
    }

    public void ResetZoom(float duration)
    {
        StartZoom(defaultZoom, duration);
    }

    public void Shake(float duration, float strength)
    {
        StopShake();
        if (cameraController == null || duration <= 0f || strength <= 0f) return;
        shakeRoutine = StartCoroutine(ShakeRoutine(duration, strength));
    }

    public void StartShake(float strength)
    {
        StopShake();
        if (cameraController == null || strength <= 0f) return;
        shakeRoutine = StartCoroutine(ContinuousShakeRoutine(strength));
    }

    public void StopShake()
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }
        if (cameraController != null) cameraController.EffectOffset = Vector2.zero;
    }

    private void StartZoom(float targetZoom, float duration)
    {
        if (targetCamera == null) return;
        if (zoomRoutine != null) StopCoroutine(zoomRoutine);
        zoomRoutine = StartCoroutine(ZoomRoutine(targetZoom, duration));
    }

    private IEnumerator ZoomRoutine(float targetZoom, float duration)
    {
        float startZoom = GetCurrentZoom();
        targetZoom = targetCamera.orthographic
            ? Mathf.Max(0.01f, targetZoom)
            : Mathf.Clamp(targetZoom, 1f, 179f);

        if (duration <= 0f)
        {
            ApplyZoom(targetZoom);
            zoomRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += DeltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            ApplyZoom(Mathf.Lerp(startZoom, targetZoom, progress));
            yield return null;
        }

        ApplyZoom(targetZoom);
        zoomRoutine = null;
    }

    private IEnumerator ShakeRoutine(float duration, float strength)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += DeltaTime;
            float remaining = 1f - Mathf.Clamp01(elapsed / duration);
            cameraController.EffectOffset = Random.insideUnitCircle * strength * remaining;
            yield return null;
        }

        cameraController.EffectOffset = Vector2.zero;
        shakeRoutine = null;
    }

    private IEnumerator ContinuousShakeRoutine(float strength)
    {
        while (true)
        {
            cameraController.EffectOffset = Random.insideUnitCircle * strength;
            yield return null;
        }
    }

    private float GetCurrentZoom()
    {
        return targetCamera.orthographic
            ? targetCamera.orthographicSize
            : targetCamera.fieldOfView;
    }

    private void ApplyZoom(float zoom)
    {
        if (targetCamera.orthographic) targetCamera.orthographicSize = zoom;
        else targetCamera.fieldOfView = zoom;
    }

    private float DeltaTime => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

    private void OnDisable()
    {
        if (zoomRoutine != null)
        {
            StopCoroutine(zoomRoutine);
            zoomRoutine = null;
        }
        StopShake();
        if (targetCamera != null) ApplyZoom(defaultZoom);
    }
}
