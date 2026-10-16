using System;
using System.Collections.Generic;
using UnityEngine;

public class GameSession : Singletion<GameSession>
{
    protected override bool PersistAcrossScenes => true;

    [Header("Game Progress")]
    [SerializeField, Min(1)]
    private int day = 1;

    [SerializeField]
    private EDayPhase phase = EDayPhase.Morning;

    [SerializeField, Min(0)]
    private int money;

    [Header("Equipment Upgrades")]
    [SerializeField, Min(0)] private int oxygenTankLevelIndex;
    [SerializeField, Min(0)] private int storageLevelIndex;
    [SerializeField, Min(0)] private int harpoonLevelIndex;

    private readonly List<FishData> fishStorage = new();

    public int Day => day;
    public EDayPhase Phase => phase;
    public int Money => money;

    public IReadOnlyList<FishData> FishStorage =>fishStorage;

    public bool CanDive => phase == EDayPhase.Morning || phase == EDayPhase.Afternoon;

    public bool CanWorkAtSushi => phase == EDayPhase.Evening;

    public event Action StateChanged;

    public void ResetNewGame()
    {
        day = 1;
        phase = EDayPhase.Morning;
        money = 0;
        oxygenTankLevelIndex = 0;
        storageLevelIndex = 0;
        harpoonLevelIndex = 0;
        fishStorage.Clear();

        NotifyStateChanged();
    }

    public bool CompleteDive( IReadOnlyList<FishData> caughtFish)
    {
        if (!CanDive)
        {
            Debug.LogWarning($"{phase} 시간에는 다이브를 할 수 없습니다.");
            return false;
        }

        StoreFish(caughtFish);

        phase = phase == EDayPhase.Morning ? EDayPhase.Afternoon : EDayPhase.Evening;

        NotifyStateChanged();
        return true;
    }

    public bool CompleteSushiShift(int earnedMoney)
    {
        if (!CanWorkAtSushi)
        {
            Debug.LogWarning("저녁에만 스시집 영업을 완료할 수 있습니다.");
            return false;
        }
        money += Mathf.Max(0, earnedMoney);
        fishStorage.Clear();

        day++;
        phase = EDayPhase.Morning;

        NotifyStateChanged();
        return true;
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0)
            return;

        money += amount;
        NotifyStateChanged();
    }

    public void AdvanceTime()
    {
        switch (phase)
        {
            case EDayPhase.Morning:
                phase = EDayPhase.Afternoon;
                break;
            case EDayPhase.Afternoon:
                phase = EDayPhase.Evening;
                break;
            case EDayPhase.Evening:
                day++;
                phase = EDayPhase.Morning;
                fishStorage.Clear();
                break;
        }

        NotifyStateChanged();
    }

    public int GetUpgradeLevelIndex(EEquipmentUpgradeType type)
    {
        return type switch
        {
            EEquipmentUpgradeType.OxygenTank => oxygenTankLevelIndex,
            EEquipmentUpgradeType.Storage => storageLevelIndex,
            EEquipmentUpgradeType.Harpoon => harpoonLevelIndex,
            _ => 0
        };
    }

    public bool TryPurchaseUpgrade(EquipmentUpgradeData upgrade)
    {
        if (upgrade == null || upgrade.LevelCount == 0)
            return false;

        int currentIndex = GetUpgradeLevelIndex(upgrade.UpgradeType);
        int nextIndex = currentIndex + 1;
        if (nextIndex >= upgrade.LevelCount)
            return false;

        EquipmentUpgradeLevel nextLevel = upgrade.GetLevel(nextIndex);
        if (nextLevel == null || money < nextLevel.Cost)
            return false;

        money -= nextLevel.Cost;
        SetUpgradeLevelIndex(upgrade.UpgradeType, nextIndex);
        NotifyStateChanged();
        return true;
    }

    public bool TryConsumeFish(FishData fish)
    {
        if (fish == null)
            return false;

        int index = fishStorage.IndexOf(fish);
        if (index < 0)
            return false;

        fishStorage.RemoveAt(index);
        NotifyStateChanged();
        return true;
    }

    private void StoreFish(
        IReadOnlyList<FishData> caughtFish)
    {
        if (caughtFish == null)
            return;

        foreach (FishData fish in caughtFish)
        {
            if (fish != null)
            {
                fishStorage.Add(fish);
            }
        }
    }

    private void SetUpgradeLevelIndex(EEquipmentUpgradeType type, int index)
    {
        index = Mathf.Max(0, index);
        switch (type)
        {
            case EEquipmentUpgradeType.OxygenTank:
                oxygenTankLevelIndex = index;
                break;
            case EEquipmentUpgradeType.Storage:
                storageLevelIndex = index;
                break;
            case EEquipmentUpgradeType.Harpoon:
                harpoonLevelIndex = index;
                break;
        }
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke();
    }
}
