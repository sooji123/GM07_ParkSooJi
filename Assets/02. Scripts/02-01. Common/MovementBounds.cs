using UnityEngine;

[CreateAssetMenu(menuName = "Game/Movement Bounds")]
public sealed class MovementBounds : ScriptableObject
{
    [SerializeField] private Vector2 min = new Vector2(-15f, -30f);
    [SerializeField] private Vector2 max = new Vector2(15f, 20f);

    public Vector2 Min => Vector2.Min(min, max);
    public Vector2 Max => Vector2.Max(min, max);

    public Vector2 Clamp(Vector2 point)
    {
        Vector2 lower = Min;
        Vector2 upper = Max;
        return new Vector2(Mathf.Clamp(point.x, lower.x, upper.x),
            Mathf.Clamp(point.y, lower.y, upper.y));
    }
}
