using System.Collections.Generic;
using UnityEngine;
// CalculateSteering copied verbatim from HEAD 6317671.
public sealed class FishSteeringBaseline {
private readonly List<FishAgent> members;
private float neighborRadius = 2f, separationRadius = 0.6f, alignmentWeight = 1f, cohesionWeight = 0.7f, separationWeight = 1.5f;
public FishSteeringBaseline(List<FishAgent> value) { members = value; }
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