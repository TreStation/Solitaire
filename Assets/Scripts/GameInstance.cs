using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Represents the current state of the game.
/// </summary>
public class GameInstance : MonoBehaviour
{
    public static GameInstance instance;

    public Difficulty Difficulty { get; set; }

    public PlayerController PlayerController { get; set; }

    public GoogleAdsManager AdsManager;
    
    public bool hasPremium = false;
    public bool hasGemBoost = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);

        SpawnPlayer();
        LoadFromDevice();
    }

    private void SpawnPlayer()
    {
        GameObject playerObject = new("Player Controller");
        playerObject.AddComponent<PlayerController>();
        PlayerController = playerObject.GetComponent<PlayerController>();
        LoadFromDevice();
    }

    public void LoadFromDevice()
    {
        PlayerData playerData = new()
        {
            Username = PlayerPrefs.GetString("Username", "TOON"),
            Gems = PlayerPrefs.GetInt("Gems", 0),
            Tickets = PlayerPrefs.GetInt("Tickets", 0),
            Background = Enum.Parse<BackgroundType>(PlayerPrefs.GetString("Background", BackgroundType.Beach.ToString())),
            Music = Enum.Parse<MusicType>(PlayerPrefs.GetString("Music", MusicType.Jazz.ToString())),
            DeckStyle = Enum.Parse<DeckType>(PlayerPrefs.GetString("DeckStyle", DeckType.Red.ToString()))
        };
        PlayerController.State = playerData;
    }
    public void SaveToDevice()
    {
        PlayerData saveData = PlayerController.State;

        PlayerPrefs.SetInt("Gems", saveData.Gems);
        PlayerPrefs.SetInt("Tickets", saveData.Tickets);
        PlayerPrefs.SetString("Background", saveData.Background.ToString());
        PlayerPrefs.SetString("Music", saveData.Music.ToString());
        PlayerPrefs.SetString("Deck", saveData.DeckStyle.ToString());

        PlayerPrefs.Save();
    }

    public void Win()
    {
        int earnedGems;
        if (PlayerPrefs.GetInt("GemBoost", 0) == 1)
        {
            earnedGems = (Difficulty == Difficulty.Easy) ? 10 : 20;
        }
        else
        {
            earnedGems = (Difficulty == Difficulty.Easy) ? 5 : 10;
        }
        PlayerController.State.Gems += earnedGems;

        SaveToDevice();
        SceneManager.LoadScene("Menu");
    }

    public void ShowAd()
    {
        if (PlayerPrefs.GetInt("Premium", 0) == 1)
        {
            AdsManager.ShowInterstitialAd();

        }
        else
        {
            SceneManager.LoadScene("Game");
        }
    }

    public void ExitToMenu()
    {
        SceneManager.LoadScene("Menu");
    }
}
