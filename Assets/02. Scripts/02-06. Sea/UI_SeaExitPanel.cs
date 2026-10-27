using UnityEngine;

public class UI_SeaExitPanel : MonoBehaviour
{
    [SerializeField] 
    private GameObject exitPanel;
    [SerializeField]
    private ESceneName targetScene;
    [SerializeField]
    private PlayerFishInventory playerFishInventory;

    private bool isOpen;

    public void Open()
    {
        if (isOpen)
        {
            return;
        }
        isOpen = true;
        if (SoundManager.HasInstance) SoundManager.Instance.PlaySfx(ESfx.UI_Open);
        if (exitPanel != null)
        {
            exitPanel.SetActive(true);
        }
        Time.timeScale = 0f;
    }
    public void Close()
    {
        if(!isOpen)
        {
            return;
        }
        isOpen = false;
        if (exitPanel != null)
        {
            exitPanel.SetActive(false);
            if (SoundManager.HasInstance) SoundManager.Instance.PlaySfx(ESfx.UI_Cancel);
        }
        Time.timeScale = 1f;
    }
    public void ExitToBoat()
    {
        Time.timeScale = 1f;

        GameFlow.CompleteDive(playerFishInventory.CaughtFish);
    }
    private void OnDisable()
    {
        isOpen = false;
        Time.timeScale = 1f;
    }
}
