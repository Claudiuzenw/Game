using UnityEngine;

// Put this on one GameObject in your first scene. It creates the services,
// loads the save, and autosaves every 30 seconds, on pause and on quit.
public class GameBootstrap : MonoBehaviour
{
    public static GameBootstrap Instance { get; private set; }

    public SaveService Saves { get; private set; }
    public PlayerSave Data => Saves != null ? Saves.Data : null;

    [SerializeField] float autosaveSeconds = 30f;
    float _timer;

    void Awake()
    {
        // Singleton Pattern: Ensure only one Bootstrap exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Initialize the Save System
        try
        {
            Saves = new SaveService();
            Saves.Load();

            if (!Saves.LoadedFromExistingSave)
            {
                Saves.Save();
            }

            UnityEngine.Debug.Log($"[BOOTSTRAP] Save loaded. Gold: {Data?.Wallet.Gold}");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"[BOOTSTRAP] Critical error initializing saves: {e}");
        }
    }

    void Update()
    {
        // The ?. operator prevents the NullReferenceException
        // It means: "Only call Save() if Saves is not null"
        if (Saves == null) return;

        _timer += Time.unscaledDeltaTime;
        if (_timer >= autosaveSeconds)
        {
            _timer = 0f;
            Saves.Save();
        }
    }

    void OnApplicationPause(bool paused)
    {
        if (paused) Saves?.Save();
    }

    void OnApplicationQuit()
    {
        Saves?.Save();
    }
}
