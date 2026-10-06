using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>플레이어가 잡은 물고기와 총 보유 무게를 관리합니다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerMotor))]
public sealed class PlayerFishInventory : MonoBehaviour
{
    [Header("무게 제한")]
    [SerializeField, Min(0f)] 
    private float overweightThreshold = 10f;
    [SerializeField, Min(0f)] 
    private float maximumCarryWeight = 15f;
    [SerializeField, Range(0.1f, 1f)] 
    private float overweightSpeedMultiplier = 0.65f;

    [Header("참조")]
    [SerializeField] 
    private PlayerMotor playerMotor;
    [SerializeField] 
    private GameObject overweightIndicator;
    [SerializeField] 
    private TMP_Text weightText;
    [SerializeField] 
    private TMP_Text weightLimitText;

    [Header("잡은 물고기")]
    [SerializeField] 
    private List<FishData> caughtFish = new();

    public IReadOnlyList<FishData> CaughtFish => caughtFish;
    public float CurrentWeight { get; private set; }
    public bool IsOverweight => CurrentWeight > overweightThreshold;

    private void Awake()
    {
        if (playerMotor == null) playerMotor = GetComponent<PlayerMotor>();
        RecalculateWeight();
        RefreshState();
    }

    /// <summary>물고기를 추가했을 때 최대 보관 무게를 넘지 않는지 확인합니다.</summary>
    public bool CanStore(FishData fishData)
    {
        if (fishData == null) return false;

        return CurrentWeight + Mathf.Max(0f, fishData.Weight) <= Mathf.Max(overweightThreshold, maximumCarryWeight) + 0.001f;
    }

    /// <summary>보관 가능한 물고기를 저장하고 무게 효과와 UI를 갱신합니다.</summary>
    public bool TryStore(FishData fishData)
    {
        if (!CanStore(fishData)) return false;

        caughtFish.Add(fishData);
        CurrentWeight += Mathf.Max(0f, fishData.Weight);
        RefreshState();
        return true;
    }

    private void RecalculateWeight()
    {
        CurrentWeight = 0f;
        foreach (FishData fishData in caughtFish)
        {
            if (fishData == null) continue;
            CurrentWeight += Mathf.Max(0f, fishData.Weight);
        }
    }

    private void RefreshState()
    {
        bool overweight = IsOverweight;
        if (overweightIndicator != null) overweightIndicator.SetActive(overweight);
        if (playerMotor != null)
            playerMotor.SetCarryWeightSpeedMultiplier(overweight ? overweightSpeedMultiplier : 1f);
        if (weightText != null) weightText.text = $"{CurrentWeight:0.0}";
        if (weightLimitText != null) weightLimitText.text = $"/{overweightThreshold:0.0}kg";
    }

    private void OnValidate()
    {
        maximumCarryWeight = Mathf.Max(overweightThreshold, maximumCarryWeight);
    }
}
