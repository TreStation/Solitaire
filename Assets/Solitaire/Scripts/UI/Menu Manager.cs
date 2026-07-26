using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;


public class MenuManager : MonoBehaviour
{
    private string username;
    private string password;
    private string confirmationPassword;

    [SerializeField] private GameObject usernameScreen;
    [SerializeField] private GameObject passwordScreen;
    [SerializeField] private GameObject accountScreen;
    [SerializeField] private GameObject menuScreen;
    [SerializeField] private GameObject profileScreen;
    
    private void OnEnable()
    {
        // Pre-warm the ad system as soon as the menu appears. This kicks off the
        // (asynchronous) SDK initialization and the first interstitial load in the
        // background, so an ad is ready by the time the player presses Play.
        // Without this, the ad is requested and shown in the same instant inside
        // EnterGame() — which only works in the Editor (synchronous placeholder
        // ads) and never on a real device (where init + load are async/network).
        AdManager.Instance.Initialize();
        
        // Listen to auth events
        AuthManager.instance.OnSignedIn += GoToMenuScreen;
        AuthManager.instance.OnAuthError += OnAuthError;

        _ = InitializeMenuAsync();
    }

    private void OnDisable()
    {
        AuthManager.instance.OnSignedIn -= GoToMenuScreen;
        AuthManager.instance.OnAuthError -= OnAuthError;
        
    }

    private async Task InitializeMenuAsync()
    {
        bool hasPlayerInfo = await AuthManager.instance.InitializeAndRefreshPlayerInfoAsync();

        if (hasPlayerInfo && AuthManager.instance.HasAccountLinked)
        {
            GoToMenuScreen();
            return;
        }

        if (hasPlayerInfo)
        {
            OnAuthError("Sign in or create an account to continue.");
        }
    }

    public void GoToMenuScreen()
    {
        Debug.Log("[MenuManager] Signed in");
        
        // Show Menu Screen
        DeactivateScreens();
        menuScreen.SetActive(true);
    }

    public void GoToAccountScreen()
    {
        Debug.Log("[MenuManager] Returning to Account Screen");
        
        DeactivateScreens();
        accountScreen.SetActive(true);
    }

    private void DeactivateScreens()
    {
        usernameScreen.SetActive(false);
        passwordScreen.SetActive(false);
        accountScreen.SetActive(false);
        profileScreen.SetActive(false);
        menuScreen.SetActive(false);
    }

    #region  Login
    // Getters
    public string GetUsername() => username;
    public string GetPassword() => password;
    // Setters
    public void SetUsername(TMP_InputField inputUsername) 
    {
        username = inputUsername.text;
        Debug.Log($"[MenuManager] Set Username to {username}");
    }
        
    public void SetPassword(TMP_InputField inputPassword)
    {
        // Store Password
        password = inputPassword.text;
    }
    public void SetConfirmationPassword(TMP_InputField inputConfirmationPassword)
    {
        confirmationPassword = inputConfirmationPassword.text;
    }
    public async void CreateAccount()
    {
        bool isPasswordValid = CheckPasswordValidity();
        if (!isPasswordValid) return;
        
        try
        {
            Debug.Log($"[MenuManager] Registering username {username}");
            await AuthManager.instance.RegisterAsync(username, password);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Registration failed: {ex}");
        }
    }
    
    public async void Login()
    {
        try
        {
            Debug.Log($"[MenuManager] Logging In... {username}");
            await AuthManager.instance.SignInAsync(username, password);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MenuManager] Login failed: {ex}");
        }
    }
    
    public void SignOut()
    {
        AuthManager.instance.SignOut();
        GoToAccountScreen();
    }
    #endregion

    #region Validation
    public bool CheckPasswordValidity()
    {
        if (password != confirmationPassword )
        {
            OnAuthError("Passwords do not match.");
            return false;
        }
        
        return true;
    }
    
    private void OnAuthError(string message)
    {
        // Show Login Screen
        AudioManager.instance.PlaySfx("error");
        Debug.LogWarning($"[MenuManager] {message}");
    }
    #endregion
    
    #region Enter Game
    /// <summary>Button hook: start an Easy game (draw-1 mode).</summary>
    public void EnterGameEasy()
    {
        SetDifficulty(DifficultyType.Easy);
        EnterGame();
    }
    /// <summary>Button hook: start a Hard game (draw-3 mode).</summary>
    public void EnterGameHard()
    {
        SetDifficulty(DifficultyType.Hard);
        EnterGame();
    }
    public void EnterGame()
    {
        // Show an interstitial ad first; only load the gameplay scene once the
        // ad has been closed (or immediately if no ad / SDK is available).
        AdManager.Instance.ShowInterstitial(() => SceneManager.LoadScene("Game"));
    }
    
    private void SetDifficulty(DifficultyType difficulty)
    {
        if (GameInstance.instance != null)
            GameInstance.instance.Difficulty = difficulty;
    }
    #endregion
}
