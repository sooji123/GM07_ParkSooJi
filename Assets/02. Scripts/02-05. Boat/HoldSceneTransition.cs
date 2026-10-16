using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class HoldSceneTransition : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] 
    private GameObject interactionPanel;
    [SerializeField] 
    private Image gaugeFill;

    [Tooltip("시간 제한 안내를 표시하는 공용 패널")]
    [SerializeField]
    private BoatExplanationPanel explanationPanel;

    [Header("Transition")]
    [SerializeField] 
    private ESceneName targetSceneName;
    [SerializeField, Min(0.1f)] 
    private float holdDuration = 1.5f;

    private PlayerController_Boat nearbyPlayer;
    private float holdTime;
    private bool isLoading;
    private bool diveAvailable;

    private const string NightDiveMessage = "밤에는 다이브를 할 수 없습니다.";

    private void Update()
    {
        if (nearbyPlayer == null || isLoading) return;

        RefreshAvailability();
        if (!diveAvailable) return;

        if (BoatActionLock.IsLocked)
        {
            holdTime = 0f;
            if (gaugeFill != null)
                gaugeFill.fillAmount = 0f;
            return;
        }

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
        RefreshAvailability(true);
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerController_Boat player = other.GetComponentInParent<PlayerController_Boat>();

        if (player == null || player != nearbyPlayer) return;

        nearbyPlayer = null;
        diveAvailable = false;
        if (explanationPanel != null)
        {
            explanationPanel.Hide(this);
        }
        HideAndReset();
    }

    private void RefreshAvailability(bool force = false)
    {
        bool canDive = GameSession.HasInstance && GameSession.Instance.CanDive;
        if (!force && canDive == diveAvailable)
        {
            return;
        }

        diveAvailable = canDive;
        HideAndReset();

        if (!diveAvailable)
        {
            if (explanationPanel != null)
            {
                explanationPanel.Show(this, NightDiveMessage);
            }
            return;
        }

        if (explanationPanel != null)
        {
            explanationPanel.Hide(this);
        }

        if (interactionPanel != null)
        {
            interactionPanel.SetActive(true);
        }
    }

    private void LoadTargetScene()
    {
        if (!GameSession.HasInstance || !GameSession.Instance.CanDive)
        {
            RefreshAvailability(true);
            return;
        }

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
