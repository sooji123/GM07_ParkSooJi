using System.Collections.Generic;
using UnityEngine;

/// <summary>등록된 물고기와 물고기 떼가 활동할 수 있는 구역입니다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class FishArea : MonoBehaviour
{
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

    public Vector2 GetFleeDestination(Vector2 origin, Vector2 threat, float distance)
    {
        Vector2 away = (origin - threat).normalized;
        if (away.sqrMagnitude < 0.001f) away = Vector2.right;
        Vector2 direct = ClampPoint(origin + away * distance);
        if ((direct - origin).sqrMagnitude > 0.04f) return direct;

        // 반대 방향이 경계에 막힌 경우 플레이어에게서 멀어지는 쪽의 경계 방향을 선택합니다.
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
}
