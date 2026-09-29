using UnityEngine;

[CreateAssetMenu(fileName = "FishData",menuName = "Fish/Fish Data")]
public sealed class FishData : ScriptableObject
{
    [Header("기본 정보")]
    [SerializeField]
    private string fishName;
    [SerializeField, Min(1)] 
    private int maxHealth = 10;
    [SerializeField, Min(0.01f)] 
    private float weight = 1f;
    [SerializeField] 
    private bool aggressive;

    public string FishName => fishName;
    public int MaxHealth => maxHealth;
    public float Weight => weight;
    public bool Aggressive => aggressive;
}
