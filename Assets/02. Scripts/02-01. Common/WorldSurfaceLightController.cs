using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Sends a fixed world-space water-surface height to a fullscreen Shader Graph.
/// The camera can follow the player without pinning the bright surface to the screen.
/// </summary>
[ExecuteAlways]
public sealed class WorldSurfaceLightController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform waterSurface;
    [Tooltip("The material assigned to the URP Full Screen Pass Renderer Feature.")]
    [SerializeField] private Material targetMaterial;
    [Tooltip("The global Volume that contains the Bloom override.")]
    [SerializeField] private Volume postProcessVolume;

    [Header("Fallback Surface Position")]
    [SerializeField] private float surfaceWorldY = 20f;
    [SerializeField] private float worldPlaneZ;

    [Header("Reveal Range (Viewport Y)")]
    [Tooltip("The effect starts while the surface is still slightly above the screen.")]
    [SerializeField] private float revealStart = 1.2f;
    [Tooltip("The effect reaches full strength when the surface enters this height.")]
    [SerializeField] private float revealFull = 0.88f;

    [Header("Surface Look")]
    [SerializeField, Min(0.001f)] private float whiteBandWidth = 0.015f;
    [SerializeField, Min(0.001f)] private float underwaterGlowHeight = 0.14f;
    [SerializeField, Range(0f, 1f)] private float glowStrength = 0.75f;
    [ColorUsage(true, true)]
    [SerializeField] private Color surfaceColor = new(2.2f, 2.8f, 3.2f, 1f);

    [Header("World Y Depth Fog")]
    [Tooltip("Optional. When empty, the camera Y position is used as the current ocean depth.")]
    [SerializeField] private Transform depthTarget;
    [Tooltip("World Y position that is treated as the deepest part of the map.")]
    [SerializeField] private float deepWorldY = -30f;
    [ColorUsage(true, true)]
    [SerializeField] private Color shallowFogColor = new(0.4f, 1.4f, 2.2f, 1f);
    [SerializeField] private Color deepFogColor = new(0.02f, 0.18f, 0.38f, 1f);

    [Header("World Y Bloom")]
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
