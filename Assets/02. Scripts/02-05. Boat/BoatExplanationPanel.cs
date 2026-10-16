using TMPro;
using UnityEngine;

public class BoatExplanationPanel : MonoBehaviour
{
    [SerializeField]
    private GameObject panelRoot;

    [SerializeField]
    private TMP_Text messageText;

    private Object currentOwner;

    public void Show(Object owner, string message)
    {
        currentOwner = owner;

        if (messageText != null)
        {
            messageText.text = message;
        }

        GameObject root = panelRoot != null ? panelRoot : gameObject;
        root.SetActive(true);
    }

    public void Hide(Object owner)
    {
        if (currentOwner != null && currentOwner != owner)
        {
            return;
        }

        currentOwner = null;
        GameObject root = panelRoot != null ? panelRoot : gameObject;
        root.SetActive(false);
    }
}
