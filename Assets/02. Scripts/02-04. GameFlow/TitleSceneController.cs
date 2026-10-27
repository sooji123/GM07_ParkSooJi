using UnityEngine;
using UnityEngine.UI;

public sealed class TitleSceneController : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button settingButton;
    [SerializeField] private Button closeSettingButton;
    [SerializeField] private GameObject buttonPanel;
    [SerializeField] private GameObject settingPanel;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private ESceneName firstScene = ESceneName.BoatScene;

    private bool starting;

    private void Awake()
    {
        settingPanel.SetActive(false);
        buttonPanel.SetActive(true);
        ConfigureSlider(bgmSlider);
        ConfigureSlider(sfxSlider);
    }

    private void OnEnable()
    {
        startButton.onClick.AddListener(StartGame);
        settingButton.onClick.AddListener(OpenSettings);
        closeSettingButton.onClick.AddListener(CloseSettings);
        bgmSlider.onValueChanged.AddListener(ChangeBgmVolume);
        sfxSlider.onValueChanged.AddListener(ChangeSfxVolume);
    }

    private void OnDisable()
    {
        startButton.onClick.RemoveListener(StartGame);
        settingButton.onClick.RemoveListener(OpenSettings);
        closeSettingButton.onClick.RemoveListener(CloseSettings);
        bgmSlider.onValueChanged.RemoveListener(ChangeBgmVolume);
        sfxSlider.onValueChanged.RemoveListener(ChangeSfxVolume);
    }

    public void StartGame()
    {
        if (starting) return;
        // Check both scenes before resetting an existing session.
        if (!Application.CanStreamedLevelBeLoaded(firstScene.ToString()) ||
            !Application.CanStreamedLevelBeLoaded(ESceneName.LoadingScene.ToString()))
        {
            Debug.LogError("TitleSceneController: Add the first scene and LoadingScene to the build scene list.", this);
            PlaySfx(ESfx.UI_Error);
            return;
        }

        starting = true;
        startButton.interactable = false;
        if (!GameSession.HasInstance)
            new GameObject("GameSession").AddComponent<GameSession>();
        GameSession.Instance.ResetNewGame();
        PlaySfx(ESfx.UI_Confirm);
        if (!SceneLoader.Load(firstScene))
        {
            starting = false;
            startButton.interactable = true;
        }
    }

    public void OpenSettings()
    {
        if (starting) return;
        if (SoundManager.HasInstance)
        {
            bgmSlider.SetValueWithoutNotify(SoundManager.Instance.BgmVolume);
            sfxSlider.SetValueWithoutNotify(SoundManager.Instance.SfxVolume);
        }
        buttonPanel.SetActive(false);
        settingPanel.SetActive(true);
        PlaySfx(ESfx.UI_Open);
    }

    public void CloseSettings()
    {
        settingPanel.SetActive(false);
        buttonPanel.SetActive(true);
        PlaySfx(ESfx.UI_Close);
    }

    private void ChangeBgmVolume(float value)
    {
        if (SoundManager.HasInstance) SoundManager.Instance.SetBgmVolume(value);
    }

    private void ChangeSfxVolume(float value)
    {
        if (SoundManager.HasInstance) SoundManager.Instance.SetSfxVolume(value);
    }

    private static void ConfigureSlider(Slider slider)
    {
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
    }

    private static void PlaySfx(ESfx sfx)
    {
        if (SoundManager.HasInstance) SoundManager.Instance.PlaySfx(sfx);
    }
}
