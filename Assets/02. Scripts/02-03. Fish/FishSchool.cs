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
    [Tooltip("보이드 조향을 다시 계산하는 간격입니다.")]
    [SerializeField, Range(0.1f, 0.2f)] private float steeringUpdateInterval = 0.15f;

    public FishArea Area { get; private set; }

    private readonly Dictionary<FishAgent, int> memberIndices = new();
    private readonly List<Vector2> cachedSteering = new();
    private float nextSteeringUpdateTime;

    private void Awake()
    {
        RegisterMembers();
        nextSteeringUpdateTime = Time.time + Random.Range(0f, steeringUpdateInterval);
    }

    private void Update()
    {
        if (Time.time < nextSteeringUpdateTime) return;
        nextSteeringUpdateTime = Time.time + steeringUpdateInterval;
        CalculateAllSteering();
    }

    public void AssignArea(FishArea area)
    {
        Area = area;
        RegisterMembers();
    }

    /// <summary>런타임에 생성된 물고기 떼의 구역과 구성원을 설정합니다.</summary>
    public void Configure(FishArea area, List<FishAgent> assignedMembers)
    {
        Area = area;
        members.Clear();
        if (assignedMembers != null)
            members.AddRange(assignedMembers);
        RegisterMembers();
    }

    private void RegisterMembers()
    {
        memberIndices.Clear();
        cachedSteering.Clear();
        for (int i = 0; i < members.Count; i++)
        {
            FishAgent member = members[i];
            cachedSteering.Add(Vector2.zero);
            if (member == null) continue;
            memberIndices[member] = i;
            member.AssignSchool(this);
            if (Area != null) member.AssignArea(Area);
        }
    }

    /// <summary>떼가 일정한 간격으로 미리 계산한 조향 값을 반환합니다.</summary>
    public Vector2 GetCachedSteering(FishAgent member)
    {
        return member != null && memberIndices.TryGetValue(member, out int index)
            && index < cachedSteering.Count
            ? cachedSteering[index]
            : Vector2.zero;
    }

    private void CalculateAllSteering()
    {
        float neighborRadiusSqr = neighborRadius * neighborRadius;
        float separationRadiusSqr = separationRadius * separationRadius;

        for (int selfIndex = 0; selfIndex < members.Count; selfIndex++)
        {
            FishAgent self = members[selfIndex];
            if (self == null || !self.IsAiActive)
            {
                cachedSteering[selfIndex] = Vector2.zero;
                continue;
            }

            Vector2 selfPosition = self.transform.position;
            Vector2 alignment = Vector2.zero;
            Vector2 cohesionCenter = Vector2.zero;
            Vector2 separation = Vector2.zero;
            int neighborCount = 0;

            for (int neighborIndex = 0; neighborIndex < members.Count; neighborIndex++)
            {
                if (neighborIndex == selfIndex) continue;
                FishAgent neighbor = members[neighborIndex];
                if (neighbor == null || !neighbor.IsAiActive) continue;

                Vector2 neighborPosition = neighbor.transform.position;
                Vector2 offset = neighborPosition - selfPosition;
                float distanceSqr = offset.sqrMagnitude;
                if (distanceSqr <= 0.0001f || distanceSqr > neighborRadiusSqr) continue;

                neighborCount++;
                alignment += neighbor.Velocity;
                cohesionCenter += neighborPosition;
                if (distanceSqr < separationRadiusSqr)
                    separation -= offset / Mathf.Max(distanceSqr, 0.0025f);
            }

            if (neighborCount == 0)
            {
                cachedSteering[selfIndex] = Vector2.zero;
                continue;
            }

            alignment = (alignment / neighborCount).normalized * alignmentWeight;
            Vector2 cohesion = (cohesionCenter / neighborCount - selfPosition).normalized
                * cohesionWeight;
            cachedSteering[selfIndex] = alignment + cohesion + separation * separationWeight;
        }
    }
}
