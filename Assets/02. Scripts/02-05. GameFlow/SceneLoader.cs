using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    private static ESceneName targetScene;
    private static bool hasLoadRequest;
    public static bool Load(ESceneName sceneName)
    {
        string sceneNameString = sceneName.ToString();
        string loadingName = ESceneName.LoadingScene.ToString();

        if (!Application.CanStreamedLevelBeLoaded(sceneNameString))
        {
            Debug.LogError(
                $"SceneLoader: '{sceneNameString}' 씬을 불러올 수 없습니다. " +
                "Build Profiles의 Scene List를 확인하세요."
            );

            return false;
        }
        targetScene = sceneName;
        hasLoadRequest = true;
        SceneManager.LoadScene(loadingName);
        return true;
    }
    public static bool TryConsumeTargetScene(out ESceneName sceneName)
    {
        sceneName = targetScene;
        if (!hasLoadRequest)
        {
            return false;
        }
        hasLoadRequest = false;
        return true;
    }
}
