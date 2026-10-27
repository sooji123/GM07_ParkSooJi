using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SoundManager : Singletion<SoundManager>
{
    [Serializable]
    public sealed class BgmEntry
    {
        public EBgm id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    [Serializable]
    public sealed class SfxEntry
    {
        public ESfx id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    [Serializable]
    public sealed class SceneBgmEntry
    {
        public ESceneName scene;
        public EBgm bgm;
    }

    [Header("Audio resources (assign clips here)")]
    [SerializeField] private BgmEntry[] bgmList = CreateBgmList();
    [SerializeField] private SfxEntry[] sfxList = CreateSfxList();
    [Header("Automatic scene music")]
    [SerializeField] private SceneBgmEntry[] sceneBgmList =
    {
        new SceneBgmEntry { scene = ESceneName.TitleScene, bgm = EBgm.Title },
        new SceneBgmEntry { scene = ESceneName.BoatScene, bgm = EBgm.Boat },
        new SceneBgmEntry { scene = ESceneName.DeepSeaScene, bgm = EBgm.DeepSea },
        new SceneBgmEntry { scene = ESceneName.SushiScene, bgm = EBgm.Sushi },
        new SceneBgmEntry { scene = ESceneName.LoadingScene, bgm = EBgm.Loading },
    };
    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField] private bool muted;

    private AudioSource bgmSource;
    private AudioSource sfxSource;
    private sealed class SfxLoop
    {
        public Behaviour owner;
        public AudioSource source;
        public float volume;
        public int refreshedFrame;
    }
    private readonly List<SfxLoop> sfxLoops = new List<SfxLoop>();
    private float currentBgmVolume = 1f;
    public EBgm CurrentBgm { get; private set; } = EBgm.None;
    public float BgmVolume => bgmVolume;
    public float SfxVolume => sfxVolume;
    protected override bool PersistAcrossScenes => true;

    protected override void OnSingletonInitialized()
    {
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.spatialBlend = 0f;
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
        ApplyVolume();
        SceneManager.sceneLoaded += OnSceneLoaded;
        PlaySceneBgm(SceneManager.GetActiveScene());
    }

    protected override void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
        base.OnDestroy();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single)
            PlaySceneBgm(scene);
    }

    private void PlaySceneBgm(Scene scene)
    {
        if (sceneBgmList != null)
        {
            foreach (SceneBgmEntry entry in sceneBgmList)
            {
                if (entry != null && entry.scene.ToString() == scene.name)
                {
                    PlayBgm(entry.bgm);
                    return;
                }
            }
        }
        StopBgm();
    }

    public void PlayBgm(EBgm id)
    {
        if (bgmSource == null) return;
        BgmEntry entry = bgmList == null ? null :
            Array.Find(bgmList, item => item != null && item.id == id);
        if (id == EBgm.None || entry == null || entry.clip == null)
        {
            StopBgm();
            return;
        }
        currentBgmVolume = Mathf.Clamp01(entry.volume);
        ApplyVolume();
        if (CurrentBgm == id && bgmSource.clip == entry.clip && bgmSource.isPlaying)
            return;
        CurrentBgm = id;
        bgmSource.clip = entry.clip;
        bgmSource.Play();
    }

    public void StopBgm()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
            bgmSource.clip = null;
        }
        CurrentBgm = EBgm.None;
    }

    public void PlaySfx(ESfx id)
    {
        if (sfxSource == null || sfxList == null) return;
        SfxEntry entry = Array.Find(sfxList, item => item != null && item.id == id);
        if (entry != null && entry.clip != null)
            sfxSource.PlayOneShot(entry.clip, Mathf.Clamp01(entry.volume));
    }

    public void StopSfx()
    {
        if (sfxSource != null) sfxSource.Stop();
        foreach (SfxLoop loop in sfxLoops)
            if (loop.source != null) Destroy(loop.source.gameObject);
        sfxLoops.Clear();
    }

    // Refresh from Update only while the interaction is actually charging.
    // A cancelled/completed/disabled interaction automatically stops in LateUpdate.
    public void RefreshSfxLoop(Behaviour owner, ESfx id)
    {
        if (owner == null || !owner.isActiveAndEnabled || Time.timeScale <= 0f) return;
        SfxEntry entry = sfxList == null ? null :
            Array.Find(sfxList, item => item != null && item.id == id);
        if (entry == null || entry.clip == null) return;
        SfxLoop loop = sfxLoops.Find(item => item.owner == owner);
        if (loop == null)
        {
            var audioObject = new GameObject("SFX Loop: " + id);
            audioObject.transform.SetParent(transform, false);
            var source = audioObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            loop = new SfxLoop { owner = owner, source = source };
            sfxLoops.Add(loop);
        }
        loop.refreshedFrame = Time.frameCount;
        loop.volume = Mathf.Clamp01(entry.volume);
        loop.source.volume = Mathf.Clamp01(sfxVolume) * loop.volume;
        loop.source.mute = muted;
        if (loop.source.clip != entry.clip) loop.source.clip = entry.clip;
        if (!loop.source.isPlaying) loop.source.Play();
    }

    private void LateUpdate()
    {
        for (int i = sfxLoops.Count - 1; i >= 0; i--)
        {
            SfxLoop loop = sfxLoops[i];
            if (loop.owner != null && loop.owner.isActiveAndEnabled &&
                loop.refreshedFrame == Time.frameCount && Time.timeScale > 0f) continue;
            if (loop.source != null)
            {
                loop.source.Stop();
                Destroy(loop.source.gameObject);
            }
            sfxLoops.RemoveAt(i);
        }
    }

    public void SetBgmVolume(float value) { bgmVolume = Mathf.Clamp01(value); ApplyVolume(); }
    public void SetSfxVolume(float value) { sfxVolume = Mathf.Clamp01(value); ApplyVolume(); }
    public void SetMuted(bool value) { muted = value; ApplyVolume(); }
    private void OnValidate() { ApplyVolume(); }

    private void ApplyVolume()
    {
        foreach (SfxLoop loop in sfxLoops)
        {
            if (loop.source == null) continue;
            loop.source.volume = Mathf.Clamp01(sfxVolume) * loop.volume;
            loop.source.mute = muted;
        }
        if (bgmSource != null)
        {
            bgmSource.volume = Mathf.Clamp01(bgmVolume) * currentBgmVolume;
            bgmSource.mute = muted;
        }
        if (sfxSource != null)
        {
            sfxSource.volume = Mathf.Clamp01(sfxVolume);
            sfxSource.mute = muted;
        }
    }

    private static BgmEntry[] CreateBgmList()
    {
        EBgm[] ids = (EBgm[])Enum.GetValues(typeof(EBgm));
        return Array.ConvertAll(ids, id => new BgmEntry { id = id });
    }

    private static SfxEntry[] CreateSfxList()
    {
        ESfx[] ids = (ESfx[])Enum.GetValues(typeof(ESfx));
        return Array.ConvertAll(ids, id => new SfxEntry { id = id });
    }
}
