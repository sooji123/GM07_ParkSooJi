using UnityEngine;

public sealed class SushiChefInteraction : MonoBehaviour
{
    [SerializeField] private CookingManager cookingManager;

    public bool TryTakeReadyOrder(out FoodOrder order)
    {
        order = null;
        return cookingManager != null && cookingManager.TryTakeReadyOrder(out order);
    }
}
