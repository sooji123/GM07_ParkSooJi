using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingSceneController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private Image gaugeFill;
    [Header("Loading")]
    [Tooltip("로딩 화면이 너무 빠르게 사라지는 것을 방지합니다.")]
    [SerializeField, Min(0f)]
    private float minimumDisplayTime = 0.7f;
    [Tooltip("게이지가 부드럽게 증가하는 속도입니다.")]
    [SerializeField, Min(0.1f)]
    private float gaugeSpeed = 2f;

    private IEnumerator Start()
    {
        if(gaugeFill != null)
        {
            gaugeFill.fillAmount = 0f;
        }
        yield return null;
        if(!SceneLoader.TryConsumeTargetScene(out ESceneName targetScene))
        {
            Debug.LogError("LoadingSceneController: 타겟 씬이 존재하지 않습니다.");
            yield break;

            targetScene = ESceneName.TitleScene;
        }
        yield return LoadTargetScene(targetScene);
    }
    private IEnumerator LoadTargetScene(ESceneName targetScene)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(targetScene.ToString());
        if(operation == null)
        {
            Debug.LogError($"LoadingSceneController: 씬 로드에 실패했습니다. targetScene: {targetScene}");
            yield break;
        }
        operation.allowSceneActivation = false;

        float elapsedTime = 0f;
        float displayedProgress = 0f;
        while (!operation.isDone)
        {
            elapsedTime += Time.deltaTime;
            float targetProgress = Mathf.Clamp01(operation.progress / 0.9f);
            displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress, gaugeSpeed * Time.deltaTime);
            if (gaugeFill != null)
            {
                gaugeFill.fillAmount = displayedProgress;
            }
            if (displayedProgress >= 1f && elapsedTime >= minimumDisplayTime)
            {
                operation.allowSceneActivation = true;
            }
            yield return null;
        }
    }
}
