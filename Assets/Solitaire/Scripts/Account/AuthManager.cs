using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;

public class AuthManager : MonoBehaviour
{
    public static AuthManager instance;

    /// <summary>Fired after a successful register or sign-in.</summary>
    public event Action OnSignedIn;

    /// <summary>Fired with a user-facing message when register/sign-in fails.</summary>
    public event Action<string> OnAuthError;

    /// <summary>True when Authentication Service reports a username/password credential on the current player.</summary>
    public bool HasAccountLinked { get; private set; }

    /// <summary>The authenticated Unity player ID for this session, or an empty string before sign-in.</summary>
    public string CurrentPlayerId => AuthenticationService.Instance.IsSignedIn
        ? AuthenticationService.Instance.PlayerId
        : string.Empty;

    /// <summary>The linked username reported by Authentication Service, if present.</summary>
    public string CurrentUsername => AuthenticationService.Instance.PlayerInfo?.Username ?? string.Empty;

    private Task<bool> initializePlayerTask;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Starts (or restores) the anonymous Unity Authentication session, then
    /// asks Authentication Service whether that player has a username/password
    /// credential. No local PlayerPrefs state is used for this decision.
    /// </summary>
    public Task<bool> InitializeAndRefreshPlayerInfoAsync()
    {
        // MenuManager and GameInstance can both request this during startup.
        // Share one request so Unity Services is only initialized and checked once.
        if (initializePlayerTask == null)
            initializePlayerTask = InitializeAndRefreshPlayerInfoInternalAsync();

        return initializePlayerTask;
    }

    private async Task<bool> InitializeAndRefreshPlayerInfoInternalAsync()
    {
        if (!await EnsureAnonymousSessionAsync())
            return false;

        return await RefreshPlayerInfoAsync();
    }

    // -------------------------------------------------------------------------
    // Register — link email/password to the existing anonymous player
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates a new email/password credential and links it to the player's
    /// current (anonymous) session. Call this from a "Sign up" UI — existing
    /// gems/entitlements on the anonymous session are preserved because the
    /// player ID does not change, only a credential is added to it.
    /// </summary>
    public async Task<bool> RegisterAsync(string username, string password)
    {
        
        username = CleanUsername(username);
        if (!await EnsureAnonymousSessionAsync()) return false;
        if (!await RefreshPlayerInfoAsync()) return false;
        if (HasAccountLinked)
        {
            Report("This player is already linked to a username. Please sign in instead.");
            return false;
        }
        
        try
        {
            await AuthenticationService.Instance.AddUsernamePasswordAsync(username, password);
            HasAccountLinked = true;
            initializePlayerTask = Task.FromResult(true);
            Debug.Log($"[AuthManager] Linked Account to player {AuthenticationService.Instance.PlayerId}");

            if (PurchaseManager.instance != null)
                await PurchaseManager.instance.ReloadCloudStateAsync();

            OnSignedIn?.Invoke();
            return true;
        }
        catch (AuthenticationException e) when (e.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
        {
            Report("That Account is already linked to a username. Try signing in instead.");
            return false;
        }
        catch (AuthenticationException e)
        {
            Report($"Sign-up failed: {e.Message}");
            return false;
        }
        catch (RequestFailedException e)
        {
            Report($"Sign-up failed: {e.Message}");
            return false;
        }
    }

    // -------------------------------------------------------------------------
    // Sign in — switch to an existing email-linked player
    // -------------------------------------------------------------------------

    /// <summary>
    /// Signs out of the local anonymous session and into an existing
    /// email/password account, then refreshes purchase/gem state for that
    /// account. Use this for "I already have an account" (new device, reinstall).
    /// </summary>
    public async Task<bool> SignInAsync(string email, string password)
    {
        Debug.Log($"[AuthManager] SignInAsync ENTERED with username/email '{email}'.");
        email = CleanUsername(email);
        
        try
        {
            if (AuthenticationService.Instance.IsSignedIn)
            {
                PurchaseManager.instance?.ClearActivePlayerState();
                AuthenticationService.Instance.SignOut(clearCredentials: true);
            }

            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(email, password);
            if (!await RefreshPlayerInfoAsync())
                return false;

            initializePlayerTask = Task.FromResult(true);
            Debug.Log($"[AuthManager] Signed in as player {AuthenticationService.Instance.PlayerId}");

            if (PurchaseManager.instance != null)
                await PurchaseManager.instance.ReloadCloudStateAsync();

            OnSignedIn?.Invoke();
            return true;
        }
        catch (AuthenticationException e)
        {
            Report(e.ErrorCode == AuthenticationErrorCodes.InvalidParameters
                ? "Incorrect email or password."
                : $"Sign-in failed: {e.Message}");
            return false;
        }
        catch (RequestFailedException e)
        {
            Report($"Sign-in failed: {e.Message}");
            return false;
        }
    }
    
    public void SignOut()
    {
        if (!AuthenticationService.Instance.IsSignedIn)
            return;

        PurchaseManager.instance?.ClearActivePlayerState();
        AuthenticationService.Instance.SignOut(clearCredentials: true);
        HasAccountLinked = false;
        initializePlayerTask = null;

        Debug.Log("[AuthManager] Signed out.");
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static string CleanUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return string.Empty;

        username = username.Trim();

        return new string(username
            .Where(c =>
                char.IsLetterOrDigit(c) ||
                c == '.' ||
                c == '-' ||
                c == '_' ||
                c == '@')
            .ToArray());
    }

    private async Task<bool> EnsureAnonymousSessionAsync()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            return true;
        }
        catch (Exception e)
        {
            Report($"Could not start a session: {e.Message}");
            return false;
        }
    }

    private async Task<bool> RefreshPlayerInfoAsync()
    {
        try
        {
            PlayerInfo playerInfo = await AuthenticationService.Instance.GetPlayerInfoAsync();
            HasAccountLinked = !string.IsNullOrEmpty(playerInfo.Username);
            Debug.Log($"[AuthManager] Player {CurrentPlayerId} is {(HasAccountLinked ? "linked" : "anonymous")}.");
            return true;
        }
        catch (AuthenticationException e)
        {
            Report($"Could not check account status: {e.Message}");
            return false;
        }
        catch (RequestFailedException e)
        {
            Report($"Could not check account status: {e.Message}");
            return false;
        }
    }

    private void Report(string message)
    {
        Debug.LogWarning($"[AuthManager] {message}");
        OnAuthError?.Invoke(message);
    }
}
