using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Represents the current state of the game.
/// </summary>
public class GameInstance : MonoBehaviour
{
    // Singleton instance of the GameInstance class
    public static GameInstance instance;

    // Properties
    public DifficultyType Difficulty { get; set; }
    public PlayerController PlayerController { get; set; }
    public GoogleAdsManager AdsManager;

    private void Awake()
    {
        if (instance != null) return;

        instance = this;
        DontDestroyOnLoad(gameObject);

        SpawnPlayer();
        LoadFromDevice();
    }

    private void SpawnPlayer()
    {
        GameObject playerControllerObject = new("Player Controller");
        playerControllerObject.AddComponent<PlayerController>();
        PlayerController = playerControllerObject.GetComponent<PlayerController>();
        PlayerData playerData = LoadFromDevice();
        PlayerController.State = playerData;
    }

    public PlayerData LoadFromDevice()
    {
        string defaultUsername = "SolitairePlayer123";
        string defaultBackground = BackgroundType.Beach.ToString();
        string defaultMusic = MusicType.Jazz.ToString();
        string defaultDeckStyle = DeckType.Red.ToString();

        PlayerData playerData = new()
        {
            Username = PlayerPrefs.GetString("Username", defaultUsername),
            Gems = PlayerPrefs.GetInt("Gems", 0),
            Tickets = PlayerPrefs.GetInt("Tickets", 0),
            Background = Enum.Parse<BackgroundType>(PlayerPrefs.GetString("Background", defaultBackground)),
            Music = Enum.Parse<MusicType>(PlayerPrefs.GetString("Music", defaultMusic)),
            DeckStyle = Enum.Parse<DeckType>(PlayerPrefs.GetString("DeckStyle", defaultDeckStyle)),
            Premium = PlayerPrefs.GetInt("Premium", 0) == 1,
            GemBoost = PlayerPrefs.GetInt("GemBoost", 0) == 1
        };
        return playerData;
    }

    public void SaveToDevice()
    {
        PlayerData playerData = PlayerController.State;

        PlayerPrefs.SetInt("Gems", playerData.Gems);
        PlayerPrefs.SetInt("Tickets", playerData.Tickets);
        PlayerPrefs.SetString("Background", playerData.Background.ToString());
        PlayerPrefs.SetString("Music", playerData.Music.ToString());
        PlayerPrefs.SetString("Deck", playerData.DeckStyle.ToString());
        PlayerPrefs.SetInt("Premium", playerData.Premium ? 1 : 0);
        PlayerPrefs.SetInt("GemBoost", playerData.GemBoost ? 1 : 0);

        PlayerPrefs.Save();
    }

    public void Win()
    {
        // Calculate earned gems based on difficulty and gem boost
        int earnedGems = (Difficulty == Difficulty.Easy) ? 5 : 10;
        bool gemBoostActive = PlayerController.State.GemBoost;
        earnedGems = gemBoostActive ? earnedGems * 2 : earnedGems;

        PlayerController.State.Gems += earnedGems;

        SaveToDevice();
        SceneManager.LoadScene("Menu");
    }

    public void ShowAd()
    {
        bool premiumActive = PlayerController.State.Premium;

        if (!premiumActive) AdsManager.ShowInterstitialAd();
        else LoadGame();
    }

    public void ExitToMenu() => SceneManager.LoadScene("Menu");
    public void LoadGame() => SceneManager.LoadScene("Game");
}
