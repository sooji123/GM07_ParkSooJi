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
        fishStorage.Clear();

        NotifyStateChanged();
    }

    public bool CompleteDive( IReadOnlyList<FishData> caughtFish)
    {
        if (!CanDive)
        {
            Debug.LogWarning( $"{phase} 시간에는 다이브를 완료할 수 없습니다.");
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
            Debug.LogWarning( "스시집 영업은 저녁에만 완료할 수 있습니다.");
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

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke();
    }
}
