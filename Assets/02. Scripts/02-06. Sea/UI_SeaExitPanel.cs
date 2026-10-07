using UnityEngine;

public class UI_SeaExitPanel : MonoBehaviour
{
    [SerializeField] 
    private GameObject exitPanel;
    [SerializeField]
    private ESceneName targetScene;

    private bool isOpen;

    public void Open()
    {
        if (isOpen)
        {
            return;
        }
        isOpen = true;
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
        }
        Time.timeScale = 1f;
    }
    public void ExitToBoat()
    {
        Time.timeScale = 1f;
        SceneLoader.Load(targetScene);
    }
    private void OnDisable()
    {
        isOpen = false;
        Time.timeScale = 1f;
    }
}
