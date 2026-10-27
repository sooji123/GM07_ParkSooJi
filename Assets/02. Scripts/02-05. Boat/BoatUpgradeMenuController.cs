using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BoatUpgradeMenuController : MonoBehaviour
{
    [Header("Menu")]
    [SerializeField] private GameObject phoneButtonObject;
    [SerializeField] private GameObject upgradeRoot;
    [SerializeField] private Button phoneButton;
    [SerializeField] private Button closeButton;
    [Header("Player")]
    [SerializeField] private PlayerController_Boat playerController;
    [Header("Upgrade Panels")]
    [SerializeField] private UpgradePanelView[] upgradePanels = new UpgradePanelView[0];

    private bool playerCouldMove;
    private bool isOpen;

    private void Awake()
    {
        if (phoneButton == null && phoneButtonObject != null)
            phoneButton = phoneButtonObject.GetComponent<Button>();
    }

    private void Start()
    {
        if (phoneButton != null)
            phoneButton.onClick.AddListener(Open);
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        foreach (UpgradePanelView panel in upgradePanels)
        {
            if (panel != null)
                panel.Bind(this);
        }

        SetOpenState(false);
    }

    private void OnDestroy()
    {
        if (phoneButton != null)
            phoneButton.onClick.RemoveListener(Open);
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);

        foreach (UpgradePanelView panel in upgradePanels)
        {
            if (panel != null)
                panel.Unbind();
        }

        ReleasePlayer();
    }

    public void Open()
    {
        if (isOpen)
            return;

        playerCouldMove = playerController == null || playerController.CanMove;
        isOpen = true;
        BoatActionLock.Lock(this);
        if (playerController != null)
            playerController.CanMove = false;

        SetOpenState(true);
        if (SoundManager.HasInstance) SoundManager.Instance.PlaySfx(ESfx.UI_Open);
        RefreshPanels();
    }

    public void Close()
    {
        if (!isOpen)
            return;

        SetOpenState(false);
        if (SoundManager.HasInstance) SoundManager.Instance.PlaySfx(ESfx.UI_Close);
        ReleasePlayer();
    }

    public void TryPurchase(UpgradePanelView panel)
    {
        if (!isOpen || panel == null || panel.UpgradeData == null || !GameSession.HasInstance)
            return;

        if (GameSession.Instance.TryPurchaseUpgrade(panel.UpgradeData))
        {
            if (SoundManager.HasInstance) SoundManager.Instance.PlaySfx(ESfx.Upgrade);
            RefreshPanels();
        }
        else if (SoundManager.HasInstance)
            SoundManager.Instance.PlaySfx(ESfx.UI_Error);
    }

    private void RefreshPanels()
    {
        foreach (UpgradePanelView panel in upgradePanels)
        {
            if (panel != null)
                panel.Refresh();
        }
    }

    private void SetOpenState(bool open)
    {
        if (upgradeRoot != null)
            upgradeRoot.SetActive(open);
        if (phoneButtonObject != null)
            phoneButtonObject.SetActive(!open);
    }

    private void ReleasePlayer()
    {
        BoatActionLock.Unlock(this);
        if (isOpen && playerController != null)
            playerController.CanMove = playerCouldMove;
        isOpen = false;
    }
}
