using System.Collections.Generic;
using UnityEngine;

public static class GameFlow
{
    public static bool TryStartDive()
    {
        GameSession session = GameSession.Instance;

        if (session == null || !session.CanDive)
        {
            Debug.Log("현재 시간에는 다이브할 수 없습니다.");
            return false;
        }

        return SceneLoader.Load(
            ESceneName.DeepSeaScene
        );
    }

    public static bool CompleteDive(
        IReadOnlyList<FishData> caughtFish)
    {
        GameSession session = GameSession.Instance;

        if (session == null || !session.CanDive)
            return false;

        if (!session.CompleteDive(caughtFish))
            return false;

        return SceneLoader.Load(
            ESceneName.BoatScene
        );
    }

    public static bool TryEnterSushi()
    {
        GameSession session = GameSession.Instance;

        if (session == null ||
            !session.CanWorkAtSushi)
        {
            Debug.Log("스시집은 저녁에만 갈 수 있습니다.");
            return false;
        }

        return SceneLoader.Load(
            ESceneName.SushiScene
        );
    }

    public static bool CompleteSushiShift(
        int earnedMoney)
    {
        GameSession session = GameSession.Instance;

        if (session == null)
            return false;

        if (!session.CompleteSushiShift(earnedMoney))
            return false;

        return SceneLoader.Load(
            ESceneName.BoatScene
        );
    }
}
