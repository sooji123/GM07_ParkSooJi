using UnityEngine;

[CreateAssetMenu(fileName = "FishDeathSettings", menuName = "Fish/Death Settings")]
public sealed class FishDeathSettings : ScriptableObject
{
    [Header("사망 연출")]
    [SerializeField] private Color deadColor = new(0.3f, 0.3f, 0.3f, 1f);
    [SerializeField, Min(0f)] private float despawnDelay = 10f;

    public Color DeadColor => deadColor;
    public float DespawnDelay => Mathf.Max(0f, despawnDelay);
}
