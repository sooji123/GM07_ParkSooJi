using System.Collections.Generic;
using UnityEngine;

/// <summary>등록된 물고기 구성원에게 보이드 기반 떼 조향을 제공합니다.</summary>
[DisallowMultipleComponent]
public sealed class FishSchool : MonoBehaviour
{
    [SerializeField] private List<FishAgent> members = new();
    [SerializeField, Min(0.1f)] private float neighborRadius = 2f;
    [SerializeField, Min(0.05f)] private float separationRadius = 0.6f;
    [SerializeField, Min(0f)] private float alignmentWeight = 1f;
    [SerializeField, Min(0f)] private float cohesionWeight = 0.7f;
    [SerializeField, Min(0f)] private float separationWeight = 1.5f;

    public FishArea Area { get; private set; }

    private void Awake() => RegisterMembers();

    public void AssignArea(FishArea area)
    {
        Area = area;
        RegisterMembers();
    }

    private void RegisterMembers()
    {
        foreach (FishAgent member in members)
        {
            if (member == null) continue;
            member.AssignSchool(this);
            if (Area != null) member.AssignArea(Area);
        }
    }

    public Vector2 CalculateSteering(FishAgent self)
    {
        Vector2 alignment = Vector2.zero;
        Vector2 cohesionCenter = Vector2.zero;
        Vector2 separation = Vector2.zero;
        int neighborCount = 0;

        foreach (FishAgent member in members)
        {
            if (member == null || member == self || !member.isActiveAndEnabled) continue;
            Vector2 offset = (Vector2)member.transform.position - (Vector2)self.transform.position;
            float distance = offset.magnitude;
            if (distance <= 0f || distance > neighborRadius) continue;

            neighborCount++;
            alignment += member.Velocity;
            cohesionCenter += (Vector2)member.transform.position;
            if (distance < separationRadius)
                separation -= offset.normalized / Mathf.Max(distance, 0.05f);
        }

        if (neighborCount == 0) return Vector2.zero;
        alignment = (alignment / neighborCount).normalized * alignmentWeight;
        Vector2 cohesion = (cohesionCenter / neighborCount - (Vector2)self.transform.position).normalized
            * cohesionWeight;
        return alignment + cohesion + separation * separationWeight;
    }
}
