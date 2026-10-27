using System;
using System.Collections.Generic;
using UnityEngine;

public enum EEquipmentUpgradeType
{
    OxygenTank,
    Storage,
    Harpoon
}

[Serializable]
public sealed class EquipmentUpgradeLevel
{
    [SerializeField, Min(1)] private int level = 1;
    [SerializeField, Min(0f)] private float value;
    [SerializeField, Min(0)] private int cost;

    public int Level => level;
    public float Value => value;
    public int Cost => cost;
}

[CreateAssetMenu(fileName = "EquipmentUpgrade", menuName = "Game/Equipment Upgrade")]
public sealed class EquipmentUpgradeData : ScriptableObject
{
    [SerializeField] private EEquipmentUpgradeType upgradeType;
    [SerializeField] private List<EquipmentUpgradeLevel> levels = new();

    public EEquipmentUpgradeType UpgradeType => upgradeType;
    public int LevelCount => levels == null ? 0 : levels.Count;

    public EquipmentUpgradeLevel GetLevel(int index)
    {
        if (levels == null || levels.Count == 0)
            return null;

        return levels[Mathf.Clamp(index, 0, levels.Count - 1)];
    }

    public static EquipmentUpgradeData Load(EEquipmentUpgradeType type)
    {
        if (GameSession.HasInstance &&
            GameSession.Instance.TryGetUpgradeData(type, out EquipmentUpgradeData registeredUpgrade))
        {
            return registeredUpgrade;
        }

        EquipmentUpgradeData[] allUpgrades = Resources.LoadAll<EquipmentUpgradeData>("Upgrades");
        foreach (EquipmentUpgradeData upgrade in allUpgrades)
        {
            if (upgrade != null && upgrade.UpgradeType == type)
                return upgrade;
        }

        return null;
    }

    public static float GetCurrentValue(EEquipmentUpgradeType type, float fallback)
    {
        EquipmentUpgradeData upgrade = Load(type);
        if (upgrade == null || upgrade.LevelCount == 0)
            return fallback;

        int index = GameSession.HasInstance
            ? GameSession.Instance.GetUpgradeLevelIndex(type)
            : 0;
        EquipmentUpgradeLevel level = upgrade.GetLevel(index);
        return level == null ? fallback : level.Value;
    }
}
