using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class UpgradePanelView : MonoBehaviour
{
    [SerializeField] private EquipmentUpgradeData upgradeData;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TMP_Text currentLevelText;
    [SerializeField] private TMP_Text nextLevelText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private GameObject detail;
    [SerializeField] private GameObject maxText;

    private BoatUpgradeMenuController controller;
    private bool isBound;

    public EquipmentUpgradeData UpgradeData => upgradeData;

    public void Bind(BoatUpgradeMenuController owner)
    {
        controller = owner;
        if (upgradeButton == null)
            upgradeButton = GetComponent<Button>();

        if (!isBound && upgradeButton != null)
        {
            upgradeButton.onClick.AddListener(OnUpgradeClicked);
            isBound = true;
        }

        Refresh();
    }

    public void Unbind()
    {
        if (isBound && upgradeButton != null)
            upgradeButton.onClick.RemoveListener(OnUpgradeClicked);

        isBound = false;
        controller = null;
    }

    public void Refresh()
    {
        if (upgradeData == null || !GameSession.HasInstance || upgradeData.LevelCount == 0)
            return;

        int currentIndex = GameSession.Instance.GetUpgradeLevelIndex(upgradeData.UpgradeType);
        EquipmentUpgradeLevel currentLevel = upgradeData.GetLevel(currentIndex);
        UpdateLevelText(currentLevelText, currentLevel);

        bool isMax = currentIndex >= upgradeData.LevelCount - 1;
        if (detail != null)
            detail.SetActive(!isMax);
        if (maxText != null)
            maxText.SetActive(isMax);
        if (upgradeButton != null)
            upgradeButton.interactable = !isMax;

        if (isMax)
            return;

        EquipmentUpgradeLevel nextLevel = upgradeData.GetLevel(currentIndex + 1);
        UpdateLevelText(nextLevelText, nextLevel);
        if (costText != null && nextLevel != null)
            costText.text = nextLevel.Cost.ToString();
    }

    private void OnUpgradeClicked()
    {
        controller?.TryPurchase(this);
    }

    private static void UpdateLevelText(TMP_Text target, EquipmentUpgradeLevel level)
    {
        if (target == null || level == null)
            return;

        string text = target.text ?? string.Empty;
        string levelNumber = level.Level.ToString();
        string valueNumber = level.Value.ToString("0.##");

        if (string.IsNullOrWhiteSpace(text))
        {
            target.text = $"Lv. {levelNumber}\n{valueNumber}";
            return;
        }

        text = Regex.Replace(text, @"Lv\.\s*\d+", $"Lv. {levelNumber}", RegexOptions.IgnoreCase);
        string newline = text.Contains("\r\n") ? "\r\n" : "\n";
        string[] lines = text.Replace("\r\n", "\n").Split('\n');

        for (int i = lines.Length - 1; i >= 0; i--)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            lines[i] = new Regex(@"-?\d+(?:\.\d+)?").Replace(lines[i], valueNumber, 1);
            break;
        }

        target.text = string.Join(newline, lines);
    }
}
