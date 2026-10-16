using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CodexSushiSceneSetup
{
    private const string ScenePath = "Assets/01. Scenes/SushiScene.unity";
    private const string CustomerPrefabPath = "Assets/08. Prefabs/P_Customer.prefab";
    private const string CookingPrefabPath = "Assets/08. Prefabs/P_CookingUI.prefab";
    private const string DefaultFoodPath = "Assets/07. SO/DefaultFood.asset";

    [MenuItem("Tools/Codex/Setup Sushi Scene")]
    public static void Apply()
    {
        AssetDatabase.SaveAssets();

        CookingOrderView cookingPrefab = ConfigureCookingPrefab();
        CustomerController customerPrefab = ConfigureCustomerPrefab();
        CustomerController[] customerPrefabs = LoadConfiguredCustomerPrefabs(customerPrefab);
        Scene scene = GetSushiScene();

        GameObject player = FindInScene(scene, "Player");
        GameObject playerCanvas = FindInScene(scene, "PlayerCanvas");
        GameObject energyGaugeObject = FindInScene(scene, "I_EnergyGauge");
        GameObject carriedFoodObject = FindDescendant(playerCanvas, "I_Food");
        GameObject cookingArrayObject = FindInScene(scene, "CookingArray");
        GameObject seatPointsObject = FindInScene(scene, "SeatPoints");
        GameObject spawnPointObject = FindInScene(scene, "CustomerSpawnPoint");
        GameObject pathPoint1Object = FindInScene(scene, "PathPoint1");
        GameObject pathPoint2Object = FindInScene(scene, "PathPoint2");
        GameObject chefObject = FindInScene(scene, "SushiChef");
        GameObject startPanel = FindInScene(scene, "StartPanel");
        GameObject startGaugeObject = FindInScene(scene, "I_StartGauge");
        GameObject timerGaugeObject = FindInScene(scene, "I_TimerGauge") ?? FindInScene(scene, "I_Timer");
        GameObject closePanel = FindInScene(scene, "ClosePanel");

        Require(player, "Player");
        Require(playerCanvas, "PlayerCanvas");
        Require(energyGaugeObject, "I_EnergyGauge");
        Require(carriedFoodObject, "PlayerCanvas/I_Food");
        Require(cookingArrayObject, "CookingArray");
        Require(seatPointsObject, "SeatPoints");
        Require(spawnPointObject, "CustomerSpawnPoint");
        Require(pathPoint1Object, "PathPoint1");
        Require(pathPoint2Object, "PathPoint2");
        Require(chefObject, "SushiChef");
        Require(startPanel, "StartPanel");
        Require(startGaugeObject, "I_StartGauge");
        Require(timerGaugeObject, "I_Timer 또는 I_TimerGauge");
        Require(closePanel, "ClosePanel");

        Image energyGauge = RequireComponent<Image>(energyGaugeObject);
        Image carriedFoodImage = RequireComponent<Image>(carriedFoodObject);
        Image startGauge = RequireComponent<Image>(startGaugeObject);
        Image timerGauge = RequireComponent<Image>(timerGaugeObject);

        timerGaugeObject.name = "I_TimerGauge";
        startGauge.fillAmount = 0f;
        timerGauge.fillAmount = 1f;
        energyGauge.fillAmount = 1f;
        startPanel.SetActive(true);
        carriedFoodObject.SetActive(false);

        Rigidbody2D playerBody = GetOrAdd<Rigidbody2D>(player);
        playerBody.bodyType = RigidbodyType2D.Kinematic;
        playerBody.gravityScale = 0f;
        playerBody.freezeRotation = true;

        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider != null)
            playerCollider.isTrigger = true;

        SushiPlayerController playerController = GetOrAdd<SushiPlayerController>(player);
        SetReference(playerController, "energyGauge", energyGauge);

        SushiPlayerInteraction playerInteraction = GetOrAdd<SushiPlayerInteraction>(player);
        SetReference(playerInteraction, "playerController", playerController);
        SetReference(playerInteraction, "carriedFoodObject", carriedFoodObject);
        SetReference(playerInteraction, "carriedFoodImage", carriedFoodImage);

        foreach (Transform child in seatPointsObject.transform)
        {
            SeatPoint seatPoint = child.GetComponent<SeatPoint>();
            if (child.name == "Seat")
            {
                if (seatPoint == null)
                    GetOrAdd<SeatPoint>(child.gameObject);
            }
            else if (seatPoint != null)
            {
                UnityEngine.Object.DestroyImmediate(seatPoint);
            }
        }

        SeatManager seatManager = GetOrAdd<SeatManager>(seatPointsObject);

        CookingManager cookingManager = GetOrAdd<CookingManager>(cookingArrayObject);
        SetReference(cookingManager, "cookingArray", cookingArrayObject.transform);
        SetReference(cookingManager, "cookingUIPrefab", cookingPrefab);
        SetReference(cookingManager, "defaultFood", AssetDatabase.LoadAssetAtPath<FishData>(DefaultFoodPath));
        SetFloat(cookingManager, "cookingDuration", 4f);

        SushiChefInteraction chefInteraction = GetOrAdd<SushiChefInteraction>(chefObject);
        SetReference(chefInteraction, "cookingManager", cookingManager);
        Collider2D chefCollider = chefObject.GetComponent<Collider2D>();
        if (chefCollider != null)
            chefCollider.isTrigger = true;

        CustomerSpawner customerSpawner = GetOrAdd<CustomerSpawner>(spawnPointObject);
        SetReferenceArray(customerSpawner, "customerPrefabs", customerPrefabs);
        SetReference(customerSpawner, "spawnPoint", spawnPointObject.transform);
        SetReference(customerSpawner, "pathPoint1", pathPoint1Object.transform);
        SetReference(customerSpawner, "pathPoint2", pathPoint2Object.transform);
        SetReference(customerSpawner, "seatManager", seatManager);
        SetReference(customerSpawner, "cookingManager", cookingManager);
        SetFloat(customerSpawner, "minSpawnInterval", 3f);
        SetFloat(customerSpawner, "maxSpawnInterval", 7f);
        SetFloat(customerSpawner, "customerMoveSpeed", 2f);
        SetFloat(customerSpawner, "eatingDuration", 3f);

        GameObject managerObject = FindInScene(scene, "SushiGameManager");
        if (managerObject == null)
        {
            managerObject = new GameObject("SushiGameManager");
            SceneManager.MoveGameObjectToScene(managerObject, scene);
        }

        SushiShiftController shiftController = GetOrAdd<SushiShiftController>(managerObject);
        SetReference(shiftController, "startPanel", startPanel);
        SetReference(shiftController, "startGauge", startGauge);
        SetFloat(shiftController, "startHoldDuration", 1.5f);
        SetReference(shiftController, "timerGauge", timerGauge);
        SetFloat(shiftController, "shiftDuration", 90f);
        SetFloat(shiftController, "timerEndFill", 0.6f);
        SetReference(shiftController, "playerController", playerController);
        SetReference(shiftController, "customerSpawner", customerSpawner);
        SetReference(shiftController, "coinText", FindComponentInScene<TMP_Text>(scene, "T_coin"));
        SetReference(shiftController, "dateText", FindComponentInScene<TMP_Text>(scene, "T_Date"));
        SetReference(shiftController, "closePanel", closePanel);
        SetReference(shiftController, "servedCountText", RequireComponent<TMP_Text>(FindDescendant(closePanel, "T_count")));
        SetReference(shiftController, "dailyRevenueText", RequireComponent<TMP_Text>(FindDescendant(closePanel, "T_coin")));
        SetReference(shiftController, "exitButton", RequireComponent<Button>(FindDescendant(closePanel, "B_Exit")));
        SetReference(customerSpawner, "shiftController", shiftController);
        closePanel.SetActive(false);

        DisableSceneTemplate(scene, "P_Customer");
        DisableChildrenByName(cookingArrayObject.transform, "P_CookingUI");

        EditorUtility.SetDirty(player);
        EditorUtility.SetDirty(cookingArrayObject);
        EditorUtility.SetDirty(seatPointsObject);
        EditorUtility.SetDirty(spawnPointObject);
        EditorUtility.SetDirty(chefObject);
        EditorUtility.SetDirty(managerObject);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("SushiScene 설정 완료: 시작 게이지, 에너지, 손님/요리 흐름, 영업 타이머(1 → 0.6)가 연결되었습니다.");
    }

    private static CookingOrderView ConfigureCookingPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(CookingPrefabPath);
        try
        {
            CookingOrderView view = GetOrAdd<CookingOrderView>(root);
            SetReference(view, "fishIcon", RequireComponent<Image>(FindDescendant(root, "I_Icon")));
            SetReference(view, "dishImage", RequireComponent<Image>(FindDescendant(root, "I_Food")));
            SetReference(view, "gaugeFill", RequireComponent<Image>(FindDescendant(root, "I_fill")));
            PrefabUtility.SaveAsPrefabAsset(root, CookingPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return AssetDatabase.LoadAssetAtPath<CookingOrderView>(CookingPrefabPath);
    }

    private static CustomerController ConfigureCustomerPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(CustomerPrefabPath);
        try
        {
            Rigidbody2D body = GetOrAdd<Rigidbody2D>(root);
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;

            Collider2D collider = root.GetComponent<Collider2D>();
            if (collider != null)
                collider.isTrigger = true;

            CustomerController customer = GetOrAdd<CustomerController>(root);
            SetReference(customer, "spriteRenderer", root.GetComponent<SpriteRenderer>());
            SetReference(customer, "orderImage", FindDescendant(root, "I_Bubble"));
            SetReference(customer, "requestedFoodImage", RequireComponent<Image>(FindDescendant(root, "I_Food")));
            SetReference(customer, "coinImage", FindDescendant(root, "I_Coin"));
            SetReference(customer, "coinText", RequireComponent<TMP_Text>(FindDescendant(root, "T_Coin")));
            SetFloat(customer, "coinDisplayDuration", 1f);
            PrefabUtility.SaveAsPrefabAsset(root, CustomerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return AssetDatabase.LoadAssetAtPath<CustomerController>(CustomerPrefabPath);
    }

    private static CustomerController[] LoadConfiguredCustomerPrefabs(CustomerController fallback)
    {
        CustomerController[] prefabs = new CustomerController[4];
        for (int i = 0; i < prefabs.Length; i++)
        {
            prefabs[i] = AssetDatabase.LoadAssetAtPath<CustomerController>(
                $"Assets/08. Prefabs/P_Customer{i + 1}.prefab"
            );
            if (prefabs[i] == null)
                return new[] { fallback };
        }

        return prefabs;
    }

    private static Scene GetSushiScene()
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

    private static T FindComponentInScene<T>(Scene scene, string name) where T : Component
    {
        GameObject found = FindInScene(scene, name);
        return found != null ? found.GetComponent<T>() : null;
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

    private static void DisableSceneTemplate(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            GameObject found = FindDescendant(root, name);
            if (found != null)
                found.SetActive(false);
        }
    }

    private static void DisableChildrenByName(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.StartsWith(name, StringComparison.Ordinal))
                child.gameObject.SetActive(false);
        }
    }

    private static T GetOrAdd<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }

    private static T RequireComponent<T>(GameObject gameObject) where T : Component
    {
        Require(gameObject, typeof(T).Name);
        T component = gameObject.GetComponent<T>();
        if (component == null)
            throw new InvalidOperationException($"{gameObject.name}에 {typeof(T).Name} 컴포넌트가 없습니다.");
        return component;
    }

    private static void Require(UnityEngine.Object value, string name)
    {
        if (value == null)
            throw new InvalidOperationException($"SushiScene에서 {name} 오브젝트를 찾지 못했습니다.");
    }

    private static void SetReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException($"{target.GetType().Name}.{propertyName} 필드를 찾지 못했습니다.");
        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetReferenceArray<T>(UnityEngine.Object target, string propertyName, T[] values)
        where T : UnityEngine.Object
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException($"{target.GetType().Name}.{propertyName} 필드를 찾지 못했습니다.");
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetFloat(UnityEngine.Object target, string propertyName, float value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException($"{target.GetType().Name}.{propertyName} 필드를 찾지 못했습니다.");
        property.floatValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }
}
