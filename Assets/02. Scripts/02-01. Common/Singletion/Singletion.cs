using UnityEngine;

public abstract class Singletion<T> : MonoBehaviour where T : Singletion<T>
{
    public static T Instance { get; private set; }
    public static bool HasInstance => Instance != null;
    protected virtual bool PersistAcrossScenes => false;
    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = (T)this;

        if (PersistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }

        OnSingletonInitialized();
    }
    protected virtual void OnSingletonInitialized()
    {
    }

    protected virtual void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
