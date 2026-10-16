using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TransferInteraction : MonoBehaviour
{
    private enum InteractionStep
    {
        FirstPrompt,
        SushiConfirmation,
        Loading
    }

    [Header("UI")]
    [SerializeField]
    private Image transferPanelImage;

    [Tooltip("TransferPanel 바로 아래에 있는 I_Space")]
    [SerializeField]
    private GameObject firstSpaceIcon;

    [Tooltip("TransferPanel 바로 아래에 있는 I_Sushi")]
    [SerializeField]
    private GameObject sushiIcon;

    [Tooltip("시간 제한 안내를 표시하는 공용 패널")]
    [SerializeField]
    private BoatExplanationPanel explanationPanel;

    [Header("Panel Alpha")]
    [SerializeField, Range(0f, 1f)]
    private float inactiveAlpha = 0.5f;

    [SerializeField, Range(0f, 1f)]
    private float activeAlpha = 1f;

    [Header("Scene")]
    [SerializeField]
    private ESceneName targetSceneName = ESceneName.SushiScene;

    private PlayerController_Boat nearbyPlayer;
    private InteractionStep currentStep;
    private bool interactionAvailable;

    private const string ClosedMessage = "스시집은 밤에만 운영할 수 있습니다.";

    private void Update()
    {
        if (nearbyPlayer == null) return;

        RefreshAvailability();
        if (!interactionAvailable) return;
        if (BoatActionLock.IsLocked) return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null || !keyboard.spaceKey.wasPressedThisFrame)
        {
            return;
        }

        switch (currentStep)
        {
            case InteractionStep.FirstPrompt:
                ShowSushiConfirmation();
                break;

            case InteractionStep.SushiConfirmation:
                LoadSushiScene();
                break;
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
        interactionAvailable = false;
        if (explanationPanel != null)
        {
            explanationPanel.Hide(this);
        }
        ResetInteraction();
    }

    private void RefreshAvailability(bool force = false)
    {
        bool canEnterSushi = GameSession.HasInstance && GameSession.Instance.CanWorkAtSushi;
        if (!force && canEnterSushi == interactionAvailable)
        {
            return;
        }

        interactionAvailable = canEnterSushi;

        if (!interactionAvailable)
        {
            ResetInteraction();
            if (explanationPanel != null)
            {
                explanationPanel.Show(this, ClosedMessage);
            }
            return;
        }

        if (explanationPanel != null)
        {
            explanationPanel.Hide(this);
        }

        currentStep = InteractionStep.FirstPrompt;
        SetPanelAlpha(activeAlpha);

        if (firstSpaceIcon != null)
        {
            firstSpaceIcon.SetActive(true);
        }

        if (sushiIcon != null)
        {
            sushiIcon.SetActive(false);
        }
    }
    private void ShowSushiConfirmation()
    {
        currentStep = InteractionStep.SushiConfirmation;

        if (firstSpaceIcon != null)
        {
            firstSpaceIcon.SetActive(false);
        }

        if (sushiIcon != null)
        {
            sushiIcon.SetActive(true);
        }
    }
    private void LoadSushiScene()
    {
        if (!GameSession.HasInstance || !GameSession.Instance.CanWorkAtSushi)
        {
            RefreshAvailability(true);
            return;
        }

        currentStep = InteractionStep.Loading;

        if (nearbyPlayer != null)
        {
            nearbyPlayer.CanMove = false;
        }

        SceneLoader.Load(targetSceneName);
    }
    private void ResetInteraction()
    {
        currentStep = InteractionStep.FirstPrompt;

        SetPanelAlpha(inactiveAlpha);

        if (firstSpaceIcon != null)
        {
            firstSpaceIcon.SetActive(false);
        }

        if (sushiIcon != null)
        {
            sushiIcon.SetActive(false);
        }
    }
    private void SetPanelAlpha(float alpha)
    {
        if (transferPanelImage == null) return;

        Color color = transferPanelImage.color;
        color.a = alpha;
        transferPanelImage.color = color;
    }
}
