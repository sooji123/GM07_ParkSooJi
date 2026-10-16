using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class BoatTimeAdvanceButton : MonoBehaviour
{
    [SerializeField] private Button button;

    [Header("Fade")]
    [SerializeField] private Image fadeImage;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.5f;
    [SerializeField, Min(0f)] private float fadeInDuration = 0.5f;

    private bool isTransitioning;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        SetFadeAlpha(0f);
        if (fadeImage != null)
            fadeImage.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (button != null)
            button.onClick.AddListener(AdvanceTime);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(AdvanceTime);

        if (!isTransitioning)
            return;

        StopAllCoroutines();
        SetFadeAlpha(0f);
        if (fadeImage != null)
            fadeImage.gameObject.SetActive(false);
        if (button != null)
            button.interactable = true;
        BoatActionLock.Unlock(this);
        isTransitioning = false;
    }

    private void AdvanceTime()
    {
        if (isTransitioning || BoatActionLock.IsLocked || !GameSession.HasInstance)
            return;

        StartCoroutine(AdvanceTimeWithFade());
    }

    private IEnumerator AdvanceTimeWithFade()
    {
        isTransitioning = true;
        BoatActionLock.Lock(this);
        if (button != null)
            button.interactable = false;

        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            SetFadeAlpha(0f);
            yield return FadeTo(1f, fadeOutDuration);
        }

        GameSession.Instance.AdvanceTime();
        yield return null;

        if (fadeImage != null)
        {
            yield return FadeTo(0f, fadeInDuration);
            fadeImage.gameObject.SetActive(false);
        }

        if (button != null)
            button.interactable = true;
        BoatActionLock.Unlock(this);
        isTransitioning = false;
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        float startAlpha = fadeImage.color.a;
        if (duration <= 0f)
        {
            SetFadeAlpha(targetAlpha);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetFadeAlpha(Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration));
            yield return null;
        }

        SetFadeAlpha(targetAlpha);
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeImage == null)
            return;

        Color color = fadeImage.color;
        color.a = Mathf.Clamp01(alpha);
        fadeImage.color = color;
    }

    private void OnDestroy()
    {
        BoatActionLock.Unlock(this);
    }

}
