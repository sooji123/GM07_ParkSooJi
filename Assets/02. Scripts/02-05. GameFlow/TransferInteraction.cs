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

    private void Update()
    {
        if (nearbyPlayer == null) return;

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
    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerController_Boat player = other.GetComponentInParent<PlayerController_Boat>();

        if (player == null || player != nearbyPlayer) return;

        nearbyPlayer = null;
        ResetInteraction();
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
