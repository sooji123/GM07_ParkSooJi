using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 고정된 월드 좌표의 수면 높이를 전체 화면 셰이더 그래프에 전달합니다.
/// 밝은 수면이 화면에 고정되지 않도록 하면서 카메라가 플레이어를 따라가게 합니다.
/// </summary>
[ExecuteAlways]
public sealed class WorldSurfaceLightController : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform waterSurface;
    [Tooltip("URP Full Screen Pass Renderer Feature에 지정한 머티리얼입니다.")]
    [SerializeField] private Material targetMaterial;
    [Tooltip("Bloom 오버라이드가 포함된 전역 볼륨입니다.")]
    [SerializeField] private Volume postProcessVolume;

    [Header("수면 참조가 없을 때 사용할 위치")]
    [SerializeField] private float surfaceWorldY = 20f;
    [SerializeField] private float worldPlaneZ;

    [Header("표시 범위 (뷰포트 Y)")]
    [Tooltip("수면이 화면보다 약간 위에 있을 때 효과가 시작됩니다.")]
    [SerializeField] private float revealStart = 1.2f;
    [Tooltip("수면이 이 높이에 도달하면 효과가 최대 강도로 적용됩니다.")]
    [SerializeField] private float revealFull = 0.88f;

    [Header("수면 표현")]
    [SerializeField, Min(0.001f)] private float whiteBandWidth = 0.015f;
    [SerializeField, Min(0.001f)] private float underwaterGlowHeight = 0.14f;
    [SerializeField, Range(0f, 1f)] private float glowStrength = 0.75f;
    [ColorUsage(true, true)]
    [SerializeField] private Color surfaceColor = new(2.2f, 2.8f, 3.2f, 1f);

    [Header("월드 Y 기준 수심 안개")]
    [Tooltip("선택 사항입니다. 비워두면 카메라의 Y 좌표를 현재 수심 기준으로 사용합니다.")]
    [SerializeField] private Transform depthTarget;
    [Tooltip("맵에서 가장 깊은 곳으로 취급할 월드 Y 좌표입니다.")]
    [SerializeField] private float deepWorldY = -30f;
    [ColorUsage(true, true)]
    [SerializeField] private Color shallowFogColor = new(0.4f, 1.4f, 2.2f, 1f);
    [SerializeField] private Color deepFogColor = new(0.02f, 0.18f, 0.38f, 1f);

    [Header("월드 Y 기준 블룸")]
    [SerializeField, Min(0f)] private float shallowBloomIntensity = 1f;
    [SerializeField, Min(0f)] private float deepBloomIntensity = 0.1f;

    private Bloom bloom;
    private float originalBloomIntensity;
    private bool hasBloom;

    private static readonly int SurfaceScreenYId = Shader.PropertyToID("_SurfaceScreenY");
    private static readonly int SurfaceVisibilityId = Shader.PropertyToID("_SurfaceVisibility");
    private static readonly int SurfaceBandWidthId = Shader.PropertyToID("_SurfaceBandWidth");
    private static readonly int SurfaceGlowHeightId = Shader.PropertyToID("_SurfaceGlowHeight");
    private static readonly int SurfaceGlowStrengthId = Shader.PropertyToID("_SurfaceGlowStrength");
    private static readonly int SurfaceColorId = Shader.PropertyToID("_SurfaceColor");
    private static readonly int FogColorId = Shader.PropertyToID("_FogColor");

    private void OnEnable()
    {
        CacheBloom();
        UpdateShaderValues();
    }

    private void LateUpdate() => UpdateShaderValues();

    private void OnDisable()
    {
        Shader.SetGlobalFloat(SurfaceVisibilityId, 0f);
        if (targetMaterial != null)
            targetMaterial.SetFloat(SurfaceVisibilityId, 0f);

        if (hasBloom && bloom != null)
            bloom.intensity.value = originalBloomIntensity;

        bloom = null;
        hasBloom = false;
    }

    private void CacheBloom()
    {
        bloom = null;
        hasBloom = false;

        if (postProcessVolume == null)
            return;

        VolumeProfile runtimeProfile = postProcessVolume.profile;
        if (runtimeProfile == null || !runtimeProfile.TryGet(out bloom))
            return;

        originalBloomIntensity = bloom.intensity.value;
        bloom.intensity.overrideState = true;
        hasBloom = true;
    }

    private void UpdateShaderValues()
    {
        Camera cameraToUse = targetCamera != null ? targetCamera : Camera.main;
        if (cameraToUse == null)
            return;

        float worldY = waterSurface != null ? waterSurface.position.y : surfaceWorldY;
        float worldZ = waterSurface != null ? waterSurface.position.z : worldPlaneZ;
        Vector3 samplePoint = new(cameraToUse.transform.position.x, worldY, worldZ);
        float viewportY = cameraToUse.WorldToViewportPoint(samplePoint).y;

        float visibility = Mathf.InverseLerp(revealStart, revealFull, viewportY);
        visibility = Mathf.SmoothStep(0f, 1f, visibility);

        float currentWorldY = depthTarget != null
            ? depthTarget.position.y
            : cameraToUse.transform.position.y;
        float oceanDepth01 = Mathf.InverseLerp(worldY, deepWorldY, currentWorldY);
        oceanDepth01 = Mathf.SmoothStep(0f, 1f, oceanDepth01);
        Color currentFogColor = Color.Lerp(shallowFogColor, deepFogColor, oceanDepth01);

        if (!hasBloom && postProcessVolume != null)
            CacheBloom();

        if (hasBloom && bloom != null)
        {
            bloom.intensity.value = Mathf.Lerp(
                shallowBloomIntensity,
                deepBloomIntensity,
                oceanDepth01
            );
        }

        Shader.SetGlobalFloat(SurfaceScreenYId, viewportY);
        Shader.SetGlobalFloat(SurfaceVisibilityId, visibility);
        Shader.SetGlobalFloat(SurfaceBandWidthId, whiteBandWidth);
        Shader.SetGlobalFloat(SurfaceGlowHeightId, underwaterGlowHeight);
        Shader.SetGlobalFloat(SurfaceGlowStrengthId, glowStrength);
        Shader.SetGlobalColor(SurfaceColorId, surfaceColor);
        Shader.SetGlobalColor(FogColorId, currentFogColor);

        if (targetMaterial == null)
            return;

        targetMaterial.SetFloat(SurfaceScreenYId, viewportY);
        targetMaterial.SetFloat(SurfaceVisibilityId, visibility);
        targetMaterial.SetFloat(SurfaceBandWidthId, whiteBandWidth);
        targetMaterial.SetFloat(SurfaceGlowHeightId, underwaterGlowHeight);
        targetMaterial.SetFloat(SurfaceGlowStrengthId, glowStrength);
        targetMaterial.SetColor(SurfaceColorId, surfaceColor);
        targetMaterial.SetColor(FogColorId, currentFogColor);
    }
}
