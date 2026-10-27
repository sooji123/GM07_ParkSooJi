using UnityEngine;
using UnityEngine.UI;

public sealed class CookingOrderView : MonoBehaviour
{
    [SerializeField] private Image dishImage;
    [SerializeField] private Image gaugeFill;

    [Header("Cooking Complete")]
    [Tooltip("조리가 끝났을 때 스프라이트를 변경할 Image입니다.")]
    [SerializeField] private Image completedImage;
    [Tooltip("조리가 끝났을 때 표시할 스프라이트입니다.")]
    [SerializeField] private Sprite completedSprite;

    private FoodOrder order;
    private float duration;
    private float elapsed;
    private bool completedVisualApplied;

    public void Initialize(FoodOrder newOrder, float cookingDuration)
    {
        order = newOrder;
        duration = Mathf.Max(0.01f, cookingDuration);
        elapsed = 0f;
        completedVisualApplied = false;

        if (gaugeFill != null)
            gaugeFill.fillAmount = 0f;
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

        if (progress < 1f)
            return;

        order.State = FoodOrderState.Ready;
        if (SoundManager.HasInstance) SoundManager.Instance.PlaySfx(ESfx.CookingComplete);
        ApplyCompletedVisual();
    }

    private void ApplyCompletedVisual()
    {
        if (completedVisualApplied)
            return;

        completedVisualApplied = true;

        if (completedImage != null && completedSprite != null)
            completedImage.sprite = completedSprite;
    }
}
