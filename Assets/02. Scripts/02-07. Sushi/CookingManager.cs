using System.Collections.Generic;
using UnityEngine;

public sealed class CookingManager : MonoBehaviour
{
    [SerializeField] private Transform cookingArray;
    [SerializeField] private CookingOrderView cookingUIPrefab;
    [SerializeField] private FishData defaultFood;
    [SerializeField, Min(0.1f)] private float cookingDuration = 4f;

    private readonly List<FoodOrder> orders = new();
    private readonly List<FishData> uniqueFish = new();

    public bool HasAvailableFish =>
        GameSession.HasInstance &&
        (GameSession.Instance.FishStorage.Count > 0 || defaultFood != null);

    public bool TryCreateOrder(CustomerController customer, out FoodOrder order)
    {
        order = null;
        if (customer == null || cookingUIPrefab == null || cookingArray == null)
            return false;

        if (!TrySelectFish(out FishData fish, out bool consumesStoredFish))
            return false;
        if (consumesStoredFish && !GameSession.Instance.TryConsumeFish(fish))
            return false;

        order = new FoodOrder(fish, customer);
        CookingOrderView view = Instantiate(cookingUIPrefab, cookingArray);
        order.View = view;
        view.Initialize(order, cookingDuration);
        orders.Add(order);
        if (SoundManager.HasInstance) SoundManager.Instance.PlaySfx(ESfx.CookingStart);
        return true;
    }

    public bool TryTakeReadyOrder(out FoodOrder order)
    {
        order = null;

        for (int i = 0; i < orders.Count; i++)
        {
            FoodOrder candidate = orders[i];
            if (candidate == null || candidate.State != FoodOrderState.Ready)
                continue;

            orders.RemoveAt(i);
            candidate.State = FoodOrderState.Carried;

            if (candidate.View != null)
                Destroy(candidate.View.gameObject);

            candidate.View = null;
            order = candidate;
            return true;
        }

        return false;
    }

    private bool TrySelectFish(out FishData selected, out bool consumesStoredFish)
    {
        selected = null;
        consumesStoredFish = false;
        uniqueFish.Clear();

        if (!GameSession.HasInstance)
            return false;

        IReadOnlyList<FishData> storage = GameSession.Instance.FishStorage;
        foreach (FishData fish in storage)
        {
            if (fish != null && !uniqueFish.Contains(fish))
                uniqueFish.Add(fish);
        }

        if (uniqueFish.Count == 0)
        {
            selected = defaultFood;
            return selected != null;
        }

        consumesStoredFish = true;

        uniqueFish.Sort((a, b) => b.SellPrice.CompareTo(a.SellPrice));
        int totalWeight = uniqueFish.Count * (uniqueFish.Count + 1) / 2;
        int roll = Random.Range(0, totalWeight);

        for (int i = 0; i < uniqueFish.Count; i++)
        {
            int weight = uniqueFish.Count - i;
            if (roll < weight)
            {
                selected = uniqueFish[i];
                return true;
            }

            roll -= weight;
        }

        selected = uniqueFish[uniqueFish.Count - 1];
        return true;
    }
}
