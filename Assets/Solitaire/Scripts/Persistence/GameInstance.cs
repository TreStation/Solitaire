using UnityEngine;
using UnityEngine.SceneManagement;

public class GameInstance : MonoBehaviour
{
    public static GameInstance instance;

    // Session Configuration
    public DifficultyType Difficulty;
    public CardArtType CardArt = CardArtType.Palm;
    public int GameSeed;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        Application.targetFrameRate = 60;
    }

    private async void Start()
    {
        // Refresh account status at the persistent game-entry point too.
        // MenuManager shares this same request when it is present.
        if (AuthManager.instance == null)
        {
            Debug.LogWarning("[GameInstance] AuthManager is missing; cannot check account status.");
            return;
        }

        bool hasPlayerInfo = await AuthManager.instance.InitializeAndRefreshPlayerInfoAsync();
        Debug.Log($"[GameInstance] Account status check: {(hasPlayerInfo ? "completed" : "failed")}.");
    }
}
