using UnityEngine;

public sealed class PlayerAim : MonoBehaviour
{
    [SerializeField]
    private Camera aimCamera;
    [Tooltip("선택 사항입니다. 로컬 +X축이 무기 전방을 향하는 자식 회전 기준점을 연결합니다.")]
    [SerializeField]
    private Transform weaponPivot;
    [SerializeField]
    private Transform firePoint;
    [Header("조준 곡선")]
    [SerializeField, Min(0.1f)]
    private float arcRadius = 1.2f;
    [SerializeField, Range(1f, 89f)]
    private float maxAimAngle = 45f;
    [SerializeField, Min(0.01f)]
    private float arcWidth = 0.06f;
    [SerializeField, Range(8, 96)]
    private int arcSegments = 32;
    [SerializeField, Min(0.01f)]
    private float markerSize = 0.16f;
    [SerializeField]
    private Color guideColor = Color.white;
    [SerializeField]
    private string sortingLayerName = "Default";
    [SerializeField]
    private int sortingOrder = 10;
    [Tooltip("선택 사항입니다. 회전하는 발사구 대신 위치가 안정적인 기준점을 연결합니다.")]
    [SerializeField]
    private Transform aimCenter;

    private LineRenderer leftArc;
    private LineRenderer rightArc;
    private Transform marker;
    private Mesh markerMesh;
    private Material lineMaterial;
    private bool visible;
    private Vector3 weaponScale;
    private Vector3 weaponOffset;
    public bool FacingRight { get; private set; } = true;
    public Vector2 Direction { get; private set; } = Vector2.right;
    public Vector3 Origin => firePoint != null ? firePoint.position : transform.position;
    private Vector3 Center => aimCenter != null ? aimCenter.position : transform.position;

    private void Awake()
    {
        if (weaponPivot != null && weaponPivot != transform)
        {
            weaponScale = weaponPivot.localScale;
            weaponOffset = transform.InverseTransformVector(weaponPivot.position - Center);
        }
        if (aimCamera == null) aimCamera = Camera.main;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null) lineMaterial = new Material(shader);
        leftArc = CreateArc("LeftAimArc");
        rightArc = CreateArc("RightAimArc");
        var markerObject = new GameObject("AimMarker");
        markerObject.transform.SetParent(transform, false);
        marker = markerObject.transform;
        markerMesh = new Mesh { name = "AimTriangle" };
        // 삼각형의 밑변을 곡선 위에 두고 꼭지점은 로컬 +X축 바깥쪽을 향하게 합니다.
        markerMesh.vertices = new[] { Vector3.right, Vector3.up * 0.55f, Vector3.down * 0.55f };
        markerMesh.triangles = new[] { 0, 1, 2, 2, 1, 0 };
        markerMesh.colors = new[] { guideColor, guideColor, guideColor };
        markerMesh.RecalculateBounds();
        markerObject.AddComponent<MeshFilter>().sharedMesh = markerMesh;
        var renderer = markerObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = lineMaterial;
        renderer.sortingLayerName = sortingLayerName;
        renderer.sortingOrder = sortingOrder + 1;
        UpdateVisual();
        SetVisible(false);
    }

    private LineRenderer CreateArc(string objectName)
    {
        var visual = new GameObject(objectName);
        visual.transform.SetParent(transform, false);
        var arc = visual.AddComponent<LineRenderer>();
        arc.useWorldSpace = true;
        arc.sharedMaterial = lineMaterial;
        arc.numCapVertices = 6;
        arc.sortingLayerName = sortingLayerName;
        arc.sortingOrder = sortingOrder;
        return arc;
    }

    // 회전하는 무기나 발사구의 영향을 받지 않도록 방향을 계산합니다.
    public static Vector2 ClampDirection(Vector2 offset, bool facingRight, float angleLimit)
    {
        float angle = Mathf.Atan2(offset.y, Mathf.Abs(offset.x)) * Mathf.Rad2Deg;
        float limit = Mathf.Clamp(angleLimit, 1f, 89f);
        angle = Mathf.Clamp(angle, -limit, limit) * Mathf.Deg2Rad;
        return new Vector2((facingRight ? 1f : -1f) * Mathf.Cos(angle), Mathf.Sin(angle));
    }

    public bool UpdateDirection(Vector2 screenPosition)
    {
        if (aimCamera == null) aimCamera = Camera.main;
        if (aimCamera == null) return false;
        // 광선과 평면의 교점을 구하므로 원근 카메라와 직교 카메라를 모두 지원합니다.
        Plane plane = new Plane(Vector3.forward, Center);
        Ray ray = aimCamera.ScreenPointToRay(screenPosition);
        if (!plane.Raycast(ray, out float distance)) return false;
        Vector3 target = ray.GetPoint(distance);
        Vector2 delta = target - Center;
        if (delta.sqrMagnitude > 0.0001f)
        {
            if (delta.x > 0.0001f) FacingRight = true;
            else if (delta.x < -0.0001f) FacingRight = false;
            Direction = ClampDirection(delta, FacingRight, maxAimAngle);
            UpdateWeaponPose();
        }
        UpdateVisual();
        return true;
    }

    private void UpdateWeaponPose()
    {
        if (weaponPivot == null || weaponPivot == transform) return;
        // 무기가 위아래로 뒤집히지 않도록 발사구를 포함한 무기 계층 전체를 좌우 반전합니다.
        Vector3 offset = weaponOffset;
        offset.x *= FacingRight ? 1f : -1f;
        weaponPivot.position = Center + transform.TransformVector(offset);
        Vector3 scale = weaponScale;
        scale.x = Mathf.Abs(scale.x) * (FacingRight ? 1f : -1f);
        weaponPivot.localScale = scale;
        float elevation = Mathf.Atan2(Direction.y, Mathf.Abs(Direction.x)) * Mathf.Rad2Deg;
        weaponPivot.rotation = Quaternion.Euler(0f, 0f, FacingRight ? elevation : -elevation);
    }

    private void UpdateVisual()
    {
        DrawArc(leftArc, false);
        DrawArc(rightArc, true);
        marker.position = Center + (Vector3)Direction * Mathf.Max(0.1f, arcRadius);
        marker.rotation = Quaternion.Euler(0f, 0f,
            Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg);
        marker.localScale = Vector3.one * Mathf.Max(0.01f, markerSize);
        SetVisible(visible);
    }

    private void DrawArc(LineRenderer arc, bool right)
    {
        int segments = Mathf.Clamp(arcSegments, 8, 96);
        float limit = Mathf.Clamp(maxAimAngle, 1f, 89f);
        arc.positionCount = segments + 1;
        arc.startWidth = arc.endWidth = Mathf.Max(0.01f, arcWidth);
        arc.startColor = arc.endColor = guideColor;
        for (int i = 0; i <= segments; i++)
        {
            float angle = Mathf.Lerp(-limit, limit, (float)i / segments) * Mathf.Deg2Rad;
            Vector3 radial = new Vector3((right ? 1f : -1f) * Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            arc.SetPosition(i, Center + radial * Mathf.Max(0.1f, arcRadius));
        }
    }

    public void SetVisible(bool visible, bool keepWeaponVisible = false)
    {
        this.visible = visible;
        // 발사 시 가이드는 숨기고 무기는 회수가 완료될 때까지 표시합니다.
        // 참조를 잘못 연결해도 플레이어나 상위 오브젝트가 비활성화되지 않도록 합니다.
        if (weaponPivot != null && !transform.IsChildOf(weaponPivot))
        {
            weaponPivot.gameObject.SetActive(visible || keepWeaponVisible);
        }
        if (leftArc != null)
        {
            leftArc.enabled = visible && !FacingRight;
        }
        if (rightArc != null)
        {
            rightArc.enabled = visible && FacingRight;
        }
        if (marker != null)
        {
            marker.gameObject.SetActive(visible);
        }
    }

    private void OnDisable() => SetVisible(false);
    private void OnDestroy()
    {
        if (lineMaterial != null)
        {
            Destroy(lineMaterial);
        }
        if (markerMesh != null)
        {
            Destroy(markerMesh);
        }
    }
}
