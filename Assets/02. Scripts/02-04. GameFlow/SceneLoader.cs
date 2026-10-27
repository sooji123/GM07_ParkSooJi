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

        if (!Application.CanStreamedLevelBeLoaded(sceneNameString) || !Application.CanStreamedLevelBeLoaded(loadingName))
        {
            Debug.LogError(
                $"SceneLoader: '{sceneNameString}' 씬을 불러올 수 없습니다. " +
                "Build Profiles의 Scene List를 확인하세요."
            );

            return false;
        }
        targetScene = sceneName;
        hasLoadRequest = true;
        if (SoundManager.HasInstance)
        {
            ESfx sound = sceneName == ESceneName.DeepSeaScene ? ESfx.Dive
                : sceneName == ESceneName.BoatScene &&
                  SceneManager.GetActiveScene().name == ESceneName.DeepSeaScene.ToString()
                    ? ESfx.Surface : ESfx.SceneTransition;
            SoundManager.Instance.PlaySfx(sound);
        }
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
