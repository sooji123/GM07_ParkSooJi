using System.Collections.Generic;
using UnityEngine;

/// <summary>등록된 물고기와 물고기 떼가 활동할 수 있는 구역입니다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class FishArea : MonoBehaviour
{
    public const int MovementZoneColumnCount = 3;
    public const int MovementZoneRowCount = 2;
    public const int MovementZoneCount = MovementZoneColumnCount * MovementZoneRowCount;

    [SerializeField] private BoxCollider2D movementBounds;
    [SerializeField] private List<FishAgent> individualFish = new();
    [SerializeField] private List<FishSchool> schools = new();

    private void Awake()
    {
        if (movementBounds == null) movementBounds = GetComponent<BoxCollider2D>();
        RegisterMembers();
    }

    private void OnValidate()
    {
        if (movementBounds == null) movementBounds = GetComponent<BoxCollider2D>();
    }

    private void RegisterMembers()
    {
        foreach (FishAgent fish in individualFish)
            if (fish != null) fish.AssignArea(this);

        foreach (FishSchool school in schools)
            if (school != null) school.AssignArea(this);
    }

    public bool Contains(Vector2 point)
    {
        return movementBounds != null && movementBounds.OverlapPoint(point);
    }

    public Vector2 ClosestPoint(Vector2 point)
    {
        return movementBounds != null ? movementBounds.ClosestPoint(point) : point;
    }

    public Vector2 ClampPoint(Vector2 point)
    {
        if (movementBounds == null || movementBounds.OverlapPoint(point)) return point;
        Vector2 boundaryPoint = movementBounds.ClosestPoint(point);
        // 경계에 정확히 붙어 방향이 반복 전환되지 않도록 중심 쪽으로 조금 들여보냅니다.
        return Vector2.Lerp(boundaryPoint, Center, 0.03f);
    }

    /// <summary>비공격형 물고기가 구역 안에서 위협의 반대 방향으로 회피할 목적지를 반환합니다.</summary>
    public Vector2 GetFleeDestination(Vector2 origin, Vector2 threat, float distance)
    {
        Vector2 away = (origin - threat).normalized;
        if (away.sqrMagnitude < 0.001f) away = Vector2.right;

        Vector2 direct = ClampPoint(origin + away * distance);
        if ((direct - origin).sqrMagnitude > 0.04f) return direct;

        // 반대 방향이 경계에 막히면 플레이어에게서 더 멀어지는 경계 안쪽 방향을 선택합니다.
        Vector2 perpendicular = new(-away.y, away.x);
        Vector2 optionA = ClampPoint(origin + perpendicular * distance);
        Vector2 optionB = ClampPoint(origin - perpendicular * distance);
        return (optionA - threat).sqrMagnitude >= (optionB - threat).sqrMagnitude
            ? optionA
            : optionB;
    }

    public Vector2 Center => movementBounds != null
        ? (Vector2)movementBounds.bounds.center
        : (Vector2)transform.position;

    public Vector2 RandomPoint()
    {
        if (movementBounds == null) return transform.position;
        Bounds bounds = movementBounds.bounds;
        for (int i = 0; i < 12; i++)
        {
            Vector2 point = new(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y)
            );
            if (movementBounds.OverlapPoint(point)) return point;
        }
        return Center;
    }

    /// <summary>위치가 속한 좌·중·우 및 위·아래 이동 구역의 번호를 반환합니다.</summary>
    public int GetMovementZone(Vector2 point)
    {
        if (movementBounds == null) return 0;

        Bounds bounds = movementBounds.bounds;
        float normalizedX = Mathf.InverseLerp(bounds.min.x, bounds.max.x, point.x);
        float normalizedY = Mathf.InverseLerp(bounds.min.y, bounds.max.y, point.y);
        int column = Mathf.Clamp(
            Mathf.FloorToInt(normalizedX * MovementZoneColumnCount),
            0,
            MovementZoneColumnCount - 1
        );
        int row = Mathf.Clamp(
            Mathf.FloorToInt(normalizedY * MovementZoneRowCount),
            0,
            MovementZoneRowCount - 1
        );
        return column * MovementZoneRowCount + row;
    }

    /// <summary>제외할 구역을 빼고 나머지 이동 구역 중 하나를 무작위로 선택합니다.</summary>
    public int GetRandomMovementZoneExcept(int excludedZone)
    {
        if (excludedZone < 0 || excludedZone >= MovementZoneCount)
            return Random.Range(0, MovementZoneCount);

        int selected = Random.Range(0, MovementZoneCount - 1);
        return selected >= excludedZone ? selected + 1 : selected;
    }

    /// <summary>지정한 3열 2행 이동 구역 안에서 무작위 위치를 반환합니다.</summary>
    public Vector2 RandomPointInMovementZone(int zone)
    {
        if (movementBounds == null) return transform.position;

        zone = Mathf.Clamp(zone, 0, MovementZoneCount - 1);
        Bounds bounds = movementBounds.bounds;
        int column = zone / MovementZoneRowCount;
        int row = zone % MovementZoneRowCount;
        float zoneWidth = bounds.size.x / MovementZoneColumnCount;
        float zoneHeight = bounds.size.y / MovementZoneRowCount;
        float minX = bounds.min.x + zoneWidth * column;
        float maxX = minX + zoneWidth;
        float minY = bounds.min.y + zoneHeight * row;
        float maxY = minY + zoneHeight;

        for (int i = 0; i < 12; i++)
        {
            Vector2 point = new(
                Random.Range(minX, maxX),
                Random.Range(minY, maxY)
            );
            if (movementBounds.OverlapPoint(point)) return point;
        }

        Vector2 zoneCenter = new(
            (minX + maxX) * 0.5f,
            (minY + maxY) * 0.5f
        );
        return ClampPoint(zoneCenter);
    }
}
