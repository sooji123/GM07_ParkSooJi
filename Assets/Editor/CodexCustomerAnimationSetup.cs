using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CodexCustomerAnimationSetup
{
    private const string ScenePath = "Assets/01. Scenes/SushiScene.unity";
    private const string SpriteFolder = "Assets/03. Images/03-07. Sushi/Sushi Characters/Customers";
    private const string AnimationFolder = "Assets/06. Animations/06-04. Custommer";
    private const string PrefabFolder = "Assets/08. Prefabs";
    private const string BasePrefabPath = PrefabFolder + "/P_Customer.prefab";

    [MenuItem("Tools/Codex/Setup Customer Animations And Prefabs")]
    public static void Apply()
    {
        List<CustomerController> prefabs = new();

        for (int customerNumber = 1; customerNumber <= 4; customerNumber++)
        {
            string customerId = customerNumber.ToString("00");
            AnimationClip idle = CreateOrUpdateClip(customerId, "Idle");
            AnimationClip walk = CreateOrUpdateClip(customerId, "Walk");
            AnimationClip eat = CreateOrUpdateClip(customerId, "Eat");
            AnimatorController controller = CreateOrUpdateController(customerId, idle, walk, eat);
            prefabs.Add(CreateOrUpdatePrefab(customerNumber, customerId, controller));
        }

        Scene scene = GetSushiScene();
        CustomerSpawner spawner = FindComponentInScene<CustomerSpawner>(scene);
        if (spawner == null)
            throw new InvalidOperationException("SushiScene에서 CustomerSpawner를 찾지 못했습니다.");

        SetReferenceArray(spawner, "customerPrefabs", prefabs.ToArray());
        EditorUtility.SetDirty(spawner);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Customer 1~4 애니메이션, Animator Controller, 프리팹과 랜덤 스폰 배열 연결이 완료되었습니다.");
    }

    private static AnimationClip CreateOrUpdateClip(string customerId, string action)
    {
        string texturePath = $"{SpriteFolder}/Customer{customerId}_{action}.png";
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(texturePath)
            .OfType<Sprite>()
            .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
            .ToArray();

        if (sprites.Length == 0)
            throw new InvalidOperationException($"{texturePath}에서 분할된 Sprite를 찾지 못했습니다.");

        string clipPath = $"{AnimationFolder}/Customer{customerId}_{action}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        clip.name = $"Customer{customerId}_{action}";
        clip.frameRate = 8f;
        EditorCurveBinding binding = new EditorCurveBinding
        {
            path = string.Empty,
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite"
        };
        ObjectReferenceKeyframe[] frames = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            frames[i] = new ObjectReferenceKeyframe
            {
                time = i / clip.frameRate,
                value = sprites[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimatorController CreateOrUpdateController(
        string customerId,
        AnimationClip idle,
        AnimationClip walk,
        AnimationClip eat)
    {
        string controllerPath = $"{AnimationFolder}/Customer{customerId}.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        controller.parameters = Array.Empty<AnimatorControllerParameter>();
        controller.AddParameter("State", AnimatorControllerParameterType.Int);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        foreach (ChildAnimatorState childState in stateMachine.states.ToArray())
            stateMachine.RemoveState(childState.state);
        foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions.ToArray())
            stateMachine.RemoveAnyStateTransition(transition);

        AnimatorState idleState = stateMachine.AddState("Idle", new Vector3(250f, 80f));
        AnimatorState walkState = stateMachine.AddState("Walk", new Vector3(500f, 20f));
        AnimatorState eatState = stateMachine.AddState("Eat", new Vector3(500f, 140f));
        idleState.motion = idle;
        walkState.motion = walk;
        eatState.motion = eat;
        stateMachine.defaultState = idleState;

        AddAnyStateTransition(stateMachine, idleState, 0);
        AddAnyStateTransition(stateMachine, walkState, 1);
        AddAnyStateTransition(stateMachine, eatState, 2);

        EditorUtility.SetDirty(stateMachine);
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void AddAnyStateTransition(
        AnimatorStateMachine stateMachine,
        AnimatorState destination,
        int stateValue)
    {
        AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(destination);
        transition.hasExitTime = false;
        transition.duration = 0f;
        transition.canTransitionToSelf = false;
        transition.AddCondition(AnimatorConditionMode.Equals, stateValue, "State");
    }

    private static CustomerController CreateOrUpdatePrefab(
        int customerNumber,
        string customerId,
        RuntimeAnimatorController controller)
    {
        string targetPath = $"{PrefabFolder}/P_Customer{customerNumber}.prefab";
        string sourcePath = AssetDatabase.LoadAssetAtPath<GameObject>(targetPath) != null
            ? targetPath
            : BasePrefabPath;
        GameObject root = PrefabUtility.LoadPrefabContents(sourcePath);

        try
        {
            root.name = $"P_Customer{customerNumber}";
            SpriteRenderer spriteRenderer = root.GetComponent<SpriteRenderer>();
            Animator animator = root.GetComponent<Animator>();
            CustomerController customer = root.GetComponent<CustomerController>();
            BoxCollider2D collider = root.GetComponent<BoxCollider2D>();

            if (spriteRenderer == null || animator == null || customer == null)
                throw new InvalidOperationException($"{sourcePath}에 고객 필수 컴포넌트가 없습니다.");

            Sprite[] idleSprites = AssetDatabase.LoadAllAssetsAtPath(
                    $"{SpriteFolder}/Customer{customerId}_Idle.png")
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
                .ToArray();
            spriteRenderer.sprite = idleSprites[0];
            animator.runtimeAnimatorController = controller;
            SetReference(customer, "spriteRenderer", spriteRenderer);
            SetReference(customer, "animator", animator);

            if (collider != null)
            {
                Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
                collider.size = new Vector2(spriteSize.x * 0.6f, spriteSize.y * 0.88f);
                collider.offset = new Vector2(0f, spriteSize.y * 0.5f);
            }

            PrefabUtility.SaveAsPrefabAsset(root, targetPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return AssetDatabase.LoadAssetAtPath<CustomerController>(targetPath);
    }

    private static Scene GetSushiScene()
    {
        Scene active = SceneManager.GetActiveScene();
        if (active.IsValid() && active.path == ScenePath)
            return active;
        return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
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

    private static void SetReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
    {
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
        if (property == null)
            throw new InvalidOperationException($"{target.GetType().Name}.{propertyName} 필드를 찾지 못했습니다.");
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }
}
