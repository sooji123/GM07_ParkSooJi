using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CodexBoatSceneSetup
{
    private const string ScenePath = "Assets/01. Scenes/BoatScene.unity";
    private const string UpgradeFolder = "Assets/Resources/Upgrades";

    [MenuItem("Tools/Codex/Setup Boat Time And Upgrades")]
    public static void Apply()
    {
        EnsureFolders();
        EquipmentUpgradeData oxygen = EnsureUpgradeAsset(
            "OxygenTankUpgrade.asset",
            EEquipmentUpgradeType.OxygenTank,
            new[] { 80f, 110f, 150f },
            new[] { 0, 250, 650 }
        );
        EquipmentUpgradeData storage = EnsureUpgradeAsset(
            "StorageUpgrade.asset",
            EEquipmentUpgradeType.Storage,
            new[] { 10f, 16f, 24f },
            new[] { 0, 300, 750 }
        );
        EquipmentUpgradeData harpoon = EnsureUpgradeAsset(
            "HarpoonUpgrade.asset",
            EEquipmentUpgradeType.Harpoon,
            new[] { 5f, 8f, 12f },
            new[] { 0, 350, 900 }
        );

        Scene scene = GetBoatScene();
        GameObject menuPanel = Require(FindInScene(scene, "MenuPanel"), "MenuPanel");
        GameObject phoneObject = Require(FindInScene(scene, "B_Phone"), "B_Phone");
        GameObject upgradeRoot = Require(FindInScene(scene, "I_Upgrade"), "I_Upgrade");
        GameObject closeObject = Require(FindDescendant(upgradeRoot, "B_Exit"), "I_Upgrade/B_Exit");
        GameObject nextTimeObject = Require(FindInScene(scene, "B_NextTime"), "B_NextTime");
        GameObject fadeObject = Require(FindInScene(scene, "I_Fade"), "I_Fade");
        GameObject explanationObject = Require(FindInScene(scene, "ExplanationPanel"), "ExplanationPanel");

        BoatExplanationPanel explanationPanel = GetOrAdd<BoatExplanationPanel>(explanationObject);
        SetReference(explanationPanel, "panelRoot", explanationObject);
        SetReference(
            explanationPanel,
            "messageText",
            RequireComponent<TMP_Text>(Require(FindDescendant(explanationObject, "T_text"), "ExplanationPanel/T_text"))
        );

        TransferInteraction transferInteraction = RequireComponent<TransferInteraction>(
            Require(FindInScene(scene, "TransferCollider"), "TransferCollider")
        );
        HoldSceneTransition diveInteraction = RequireComponent<HoldSceneTransition>(
            Require(FindInScene(scene, "DiveCollider"), "DiveCollider")
        );
        SetReference(transferInteraction, "explanationPanel", explanationPanel);
        SetReference(diveInteraction, "explanationPanel", explanationPanel);

        Button phoneButton = RequireComponent<Button>(phoneObject);
        Button closeButton = RequireComponent<Button>(closeObject);
        RequireComponent<Button>(nextTimeObject);
        BoatTimeAdvanceButton timeAdvanceButton = GetOrAdd<BoatTimeAdvanceButton>(nextTimeObject);
        SetReference(timeAdvanceButton, "fadeImage", RequireComponent<Image>(fadeObject));
        fadeObject.SetActive(false);

        Dictionary<EEquipmentUpgradeType, EquipmentUpgradeData> dataByType = new()
        {
            { EEquipmentUpgradeType.OxygenTank, oxygen },
            { EEquipmentUpgradeType.Storage, storage },
            { EEquipmentUpgradeType.Harpoon, harpoon }
        };

        List<UpgradePanelView> panelViews = new();
        foreach (Transform child in upgradeRoot.GetComponentsInChildren<Transform>(true))
        {
            if (child.name != "UpgradePanel")
                continue;

            GameObject panel = child.gameObject;
            TMP_Text title = FindComponentInChildren<TMP_Text>(panel, "T_detailTitle");
            EEquipmentUpgradeType type = ResolveType(title != null ? title.text : string.Empty);
            UpgradePanelView view = GetOrAdd<UpgradePanelView>(panel);

            SetReference(view, "upgradeData", dataByType[type]);
            SetReference(view, "upgradeButton", RequireComponent<Button>(panel));
            SetReference(view, "currentLevelText", FindComponentInChildren<TMP_Text>(panel, "T_CurrentLevel"));
            SetReference(view, "nextLevelText", FindComponentInChildren<TMP_Text>(panel, "T_NextLevel"));
            SetReference(view, "costText", FindComponentInChildren<TMP_Text>(panel, "T_coin"));
            SetReference(view, "detail", Require(FindDescendant(panel, "Detail"), "UpgradePanel/Detail"));
            SetReference(view, "maxText", Require(FindDescendant(panel, "T_Max"), "UpgradePanel/T_Max"));
            panelViews.Add(view);
        }

        if (panelViews.Count != 3)
            throw new InvalidOperationException($"I_Upgrade 아래 UpgradePanel은 3개여야 합니다. 현재 {panelViews.Count}개입니다.");

        BoatUpgradeMenuController controller = GetOrAdd<BoatUpgradeMenuController>(menuPanel);
        SetReference(controller, "phoneButtonObject", phoneObject);
        SetReference(controller, "upgradeRoot", upgradeRoot);
        SetReference(controller, "phoneButton", phoneButton);
        SetReference(controller, "closeButton", closeButton);
        SetReference(controller, "playerController", FindComponentInScene<PlayerController_Boat>(scene));
        SetReferenceArray(controller, "upgradePanels", panelViews.ToArray());

        upgradeRoot.SetActive(false);
        phoneObject.SetActive(true);
        explanationObject.SetActive(false);

        EditorUtility.SetDirty(menuPanel);
        EditorUtility.SetDirty(nextTimeObject);
        EditorUtility.SetDirty(fadeObject);
        EditorUtility.SetDirty(explanationObject);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("BoatScene 시간 넘기기와 장비 업그레이드 UI 연결이 완료되었습니다.");
    }

    private static EquipmentUpgradeData EnsureUpgradeAsset(
        string fileName,
        EEquipmentUpgradeType type,
        IReadOnlyList<float> values,
        IReadOnlyList<int> costs)
    {
        string path = $"{UpgradeFolder}/{fileName}";
        EquipmentUpgradeData asset = AssetDatabase.LoadAssetAtPath<EquipmentUpgradeData>(path);
        if (asset != null)
            return asset;

        asset = ScriptableObject.CreateInstance<EquipmentUpgradeData>();
        AssetDatabase.CreateAsset(asset, path);

        SerializedObject serialized = new SerializedObject(asset);
        serialized.FindProperty("upgradeType").enumValueIndex = (int)type;
        SerializedProperty levels = serialized.FindProperty("levels");
        levels.arraySize = values.Count;
        for (int i = 0; i < values.Count; i++)
        {
            SerializedProperty level = levels.GetArrayElementAtIndex(i);
            level.FindPropertyRelative("level").intValue = i + 1;
            level.FindPropertyRelative("value").floatValue = values[i];
            level.FindPropertyRelative("cost").intValue = costs[i];
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static EEquipmentUpgradeType ResolveType(string title)
    {
        if (title.Contains("공기통") || title.Contains("산소통"))
            return EEquipmentUpgradeType.OxygenTank;
        if (title.Contains("적재함"))
            return EEquipmentUpgradeType.Storage;
        if (title.Contains("작살"))
            return EEquipmentUpgradeType.Harpoon;

        throw new InvalidOperationException($"업그레이드 제목에서 장비 종류를 판단할 수 없습니다: {title}");
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(UpgradeFolder))
            AssetDatabase.CreateFolder("Assets/Resources", "Upgrades");
    }

    private static Scene GetBoatScene()
    {
        Scene active = SceneManager.GetActiveScene();
        if (active.IsValid() && active.path == ScenePath)
            return active;
        return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    private static GameObject FindInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            GameObject found = FindDescendant(root, name);
            if (found != null)
                return found;
        }
        return null;
    }

    private static T FindComponentInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T found = root.GetComponentInChildren<T>(true);
            if (found != null)
                return found;
        }
        return null;
    }

    private static GameObject FindDescendant(GameObject root, string name)
    {
        if (root == null)
            return null;
        if (root.name == name)
            return root;
        foreach (Transform child in root.transform)
        {
            GameObject found = FindDescendant(child.gameObject, name);
            if (found != null)
                return found;
        }
        return null;
    }

    private static T FindComponentInChildren<T>(GameObject root, string name) where T : Component
    {
        GameObject found = FindDescendant(root, name);
        return found != null ? found.GetComponent<T>() : null;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static T RequireComponent<T>(GameObject target) where T : Component
    {
        T component = target != null ? target.GetComponent<T>() : null;
        if (component == null)
            throw new InvalidOperationException($"{target?.name ?? "null"}에 {typeof(T).Name} 컴포넌트가 없습니다.");
        return component;
    }

    private static GameObject Require(GameObject value, string name)
    {
        if (value == null)
            throw new InvalidOperationException($"BoatScene에서 {name} 오브젝트를 찾지 못했습니다.");
        return value;
    }

    private static void SetReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
    {
        if (value == null)
            throw new InvalidOperationException($"{target.GetType().Name}.{propertyName}에 연결할 오브젝트가 없습니다.");
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException($"{target.GetType().Name}.{propertyName} 필드를 찾지 못했습니다.");
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetReferenceArray<T>(UnityEngine.Object target, string propertyName, T[] values)
        where T : UnityEngine.Object
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }
}
