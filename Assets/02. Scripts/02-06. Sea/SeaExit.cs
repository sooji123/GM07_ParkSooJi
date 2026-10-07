using UnityEngine;

public class SeaExit : MonoBehaviour
{
    [SerializeField] 
    private UI_SeaExitPanel exitPanel;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out PlayerController player))
        {
            return;
        }

        exitPanel.Open();
    }
}
