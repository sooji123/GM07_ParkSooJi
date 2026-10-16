using UnityEngine;
using UnityEngine.UI;

public sealed class CookingOrderView : MonoBehaviour
{
    [SerializeField] private Image fishIcon;
    [SerializeField] private Image dishImage;
    [SerializeField] private Image gaugeFill;

    private FoodOrder order;
    private float duration;
    private float elapsed;

    public void Initialize(FoodOrder newOrder, float cookingDuration)
    {
        order = newOrder;
        duration = Mathf.Max(0.01f, cookingDuration);
        elapsed = 0f;

        if (gaugeFill != null)
            gaugeFill.fillAmount = 0f;
        if (fishIcon != null && order.Fish.FishIcon != null)
            fishIcon.sprite = order.Fish.FishIcon;
        if (dishImage != null && order.Fish.DishSprite != null)
            dishImage.sprite = order.Fish.DishSprite;
    }

    private void Update()
    {
        if (order == null || order.State != FoodOrderState.Cooking)
            return;

        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / duration);

        if (gaugeFill != null)
            gaugeFill.fillAmount = progress;

        if (progress >= 1f)
            order.State = FoodOrderState.Ready;
    }
}
