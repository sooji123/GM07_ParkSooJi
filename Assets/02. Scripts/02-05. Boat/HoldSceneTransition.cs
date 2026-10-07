using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HoldSceneTransition : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] 
    private GameObject interactionPanel;
    [SerializeField] 
    private Image gaugeFill;

    [Header("Transition")]
    [SerializeField] 
    private ESceneName targetSceneName;
    [SerializeField, Min(0.1f)] 
    private float holdDuration = 1.5f;

    private PlayerController_Boat nearbyPlayer;
    private float holdTime;
    private bool isLoading;

    private void Update()
    {
        if (nearbyPlayer == null || isLoading) return;

        Keyboard keyboard = Keyboard.current;
        bool holdingSpace = keyboard != null && keyboard.spaceKey.isPressed;

        if (holdingSpace)
        {
            holdTime += Time.deltaTime;
        }
        else
        {
            holdTime = 0f;
        }

        float progress = Mathf.Clamp01(holdTime / holdDuration);

        if (gaugeFill != null)
        {
            gaugeFill.fillAmount = progress;
        }

        if (progress >= 1f)
        {
            LoadTargetScene();
        }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController_Boat player = other.GetComponentInParent<PlayerController_Boat>();

        if (player == null) return;

        nearbyPlayer = player;
        holdTime = 0f;

        if (gaugeFill != null)
        {
            gaugeFill.fillAmount = 0f;
        }

        if (interactionPanel != null && GameSession.Instance.CanDive)
        {
            interactionPanel.SetActive(true);
        }
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerController_Boat player = other.GetComponentInParent<PlayerController_Boat>();

        if (player == null || player != nearbyPlayer) return;

        nearbyPlayer = null;
        HideAndReset();
    }
    private void LoadTargetScene()
    {
        isLoading = true;

        if (nearbyPlayer != null)
        {
            nearbyPlayer.CanMove = false;
        }

        bool loadStarted = SceneLoader.Load(targetSceneName);

        if (!loadStarted)
        {
            isLoading = false;

            if (nearbyPlayer != null)
            {
                nearbyPlayer.CanMove = true;
            }
        }
    }
    private void HideAndReset()
    {
        holdTime = 0f;

        if (gaugeFill != null)
        {
            gaugeFill.fillAmount = 0f;
        }

        if (interactionPanel != null)
        {
            interactionPanel.SetActive(false);
        }
    }
}
