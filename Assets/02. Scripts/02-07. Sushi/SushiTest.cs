using UnityEngine;
using UnityEngine.InputSystem;

public sealed class SushiShiftTest : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField, Min(0)]
    private int pricePerFish = 10;

    private bool isEndingShift;

    private void Update()
    {
        if (isEndingShift)
            return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null ||
            !keyboard.eKey.wasPressedThisFrame)
        {
            return;
        }

        EndShift();
    }

    private void EndShift()
    {
        GameSession session = GameSession.Instance;

        if (session == null)
        {
            Debug.LogError(
                "GameSession이 존재하지 않습니다.",
                this
            );

            return;
        }

        if (!session.CanWorkAtSushi)
        {
            Debug.LogWarning(
                $"현재 시간대는 {session.Phase}입니다. " +
                "스시집 영업은 저녁에만 종료할 수 있습니다.",
                this
            );

            return;
        }

        int fishCount = session.FishStorage.Count;
        int earnedMoney = fishCount * pricePerFish;

        Debug.Log(
            $"물고기 {fishCount}마리를 판매했습니다. " +
            $"수익: {earnedMoney}, " +
            $"현재 보유금: {session.Money}"
        );

        isEndingShift = true;

        bool completed =
            GameFlow.CompleteSushiShift(earnedMoney);

        if (!completed)
        {
            isEndingShift = false;

            Debug.LogError(
                "스시집 영업 종료에 실패했습니다.",
                this
            );
        }
    }
}