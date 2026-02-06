using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using GotrueClient = Supabase.Gotrue.Client;
using SupabaseClient = Supabase.Client;
using SupabaseOptions = Supabase.SupabaseOptions;

/// <summary>
/// Manager class for handling Supabase authentication in Unity
/// Install via: https://github.com/supabase-community/supabase-csharp
/// </summary>
public class SupabaseAuthManager : MonoBehaviour
{
    [Header("Supabase Configuration")]
    [Header("Supabase Configuration")]
    [SerializeField] private string supabaseUrl = "https://whariszigfgiehwkzrfo.supabase.co";
    [SerializeField] private string supabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6IndoYXJpc3ppZ2ZnaWVod2t6cmZvIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjM1ODc1NjIsImV4cCI6MjA3OTE2MzU2Mn0.QzszyldDWdGLNe_76wu4iAO1VLgCZ8IQAGO_55F_Jlc";

    private SupabaseClient supabaseClient;
    private IGotrueClient<User, Session> authClient;

    public static SupabaseAuthManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Initialize Supabase client
    /// </summary>
    public async System.Threading.Tasks.Task InitializeSupabase()
    {
        try
        {
            var options = new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = true
            };

            supabaseClient = new SupabaseClient(supabaseUrl, supabaseAnonKey, options);
            await supabaseClient.InitializeAsync();
            
            authClient = supabaseClient.Auth;

            // Listen for auth state changes
            authClient.AddStateChangedListener(OnAuthStateChanged);

            Debug.Log("Supabase initialized successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize Supabase: {e.Message}");
        }
    }
    
     /// <summary>
    /// Sign in anonymously (guest account)
    /// </summary>
    public async System.Threading.Tasks.Task<bool> SignInAnonymously()
    {
        try
        {
            var session = await authClient.SignInAnonymously();
            
            if (session != null)
            {
                Debug.Log($"Anonymous sign in successful! User ID: {session.User.Id}");
                return true;
            }
            
            Debug.LogWarning("Anonymous sign in failed: No session returned");
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"Anonymous sign in error: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Check if current user is anonymous
    /// </summary>
    public bool IsAnonymousUser()
    {
        if (authClient?.CurrentUser == null) return false;
        
        // Anonymous users have no email
        return string.IsNullOrEmpty(authClient.CurrentUser.Email);
    }
    
    /// <summary>
    /// Check if user is currently authenticated
    /// </summary>
    public bool IsUserAuthenticated()
    {
        return authClient?.CurrentUser != null;
    }

    /// <summary>
    /// Get current user
    /// </summary>
    public User GetCurrentUser()
    {
        return authClient?.CurrentUser;
    }

    /// <summary>
    /// Get current session
    /// </summary>
    public Session GetCurrentSession()
    {
        return authClient?.CurrentSession;
    }

    /// <summary>
    /// Callback for auth state changes
    /// </summary>
    private void OnAuthStateChanged(IGotrueClient<User, Session> sender, Constants.AuthState state)
    {
        Debug.Log($"Auth state changed: {state}");

        switch (state)
        {
            case Constants.AuthState.SignedIn:
                Debug.Log($"User signed in: {authClient.CurrentUser?.Email}");
                // Handle sign in (e.g., load main scene)
                break;
            case Constants.AuthState.SignedOut:
                Debug.Log("User signed out");
                // Handle sign out (e.g., return to login screen)
                break;
            case Constants.AuthState.UserUpdated:
                Debug.Log("User updated");
                break;
            case Constants.AuthState.PasswordRecovery:
                Debug.Log("Password recovery initiated");
                break;
            case Constants.AuthState.TokenRefreshed:
                Debug.Log("Token refreshed");
                break;
        }
    }
    /// <summary>
/// Auto-authenticate with platform service (Game Center or Google Play Games)
/// Call this on Start after anonymous sign-in
/// </summary>
public async System.Threading.Tasks.Task<bool> AutoAuthenticateWithPlatform()
{
    #if UNITY_IOS
        return await AuthenticateWithGameCenter();
    #elif UNITY_ANDROID
        return await AuthenticateWithGooglePlay();
    #else
        Debug.Log("Platform auto-auth not available on this platform");
        return false;
    #endif
}

#if UNITY_IOS
/// <summary>
/// Silent Game Center authentication
/// </summary>
private async System.Threading.Tasks.Task<bool> AuthenticateWithGameCenter()
{
    var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
    
    Social.localUser.Authenticate(success =>
    {
        if (success)
        {
            string playerId = Social.localUser.id;
            Debug.Log($"Game Center: Authenticated as {Social.localUser.userName}");
            
            // Link to Supabase silently
            LinkGameCenterToSupabase(playerId).ContinueWith(task => tcs.SetResult(task.Result));
        }
        else
        {
            Debug.LogWarning("Game Center: Not authenticated");
            tcs.SetResult(false);
        }
    });
    
    return await tcs.Task;
}

private async System.Threading.Tasks.Task<bool> LinkGameCenterToSupabase(string playerId)
{
    try
    {
        // Create deterministic credentials from Game Center ID
        string email = $"gc_{playerId}@internal.game";
        string password = GenerateSecurePassword(playerId);
        
        // Check if user already exists by trying to sign in
        try
        {
            var existingSession = await authClient.SignIn(email, password);
            if (existingSession != null)
            {
                Debug.Log("Game Center: Signed into existing Supabase account");
                return true;
            }
        }
        catch
        {
            // Account doesn't exist, need to link/create
        }
        
        // If we have an anonymous session, link it
        if (IsAnonymousUser())
        {
            var attributes = new UserAttributes
            {
                Email = email,
                Password = password,
                Data = new Dictionary<string, object>
                {
                    { "platform", "ios" },
                    { "platform_id", playerId },
                    { "display_name", Social.localUser.userName }
                }
            };
            
            var updatedUser = await authClient.Update(attributes);
            if (updatedUser != null)
            {
                Debug.Log("Game Center: Linked anonymous account");
                return true;
            }
        }
        else
        {
            // Create new account
            var session = await authClient.SignUp(email, password, new SignUpOptions
            {
                Data = new Dictionary<string, object>
                {
                    { "platform", "ios" },
                    { "platform_id", playerId },
                    { "display_name", Social.localUser.userName }
                }
            });
            
            if (session != null)
            {
                Debug.Log("Game Center: Created new Supabase account");
                return true;
            }
        }
        
        return false;
    }
    catch (Exception e)
    {
        Debug.LogError($"Game Center linking error: {e.Message}");
        return false;
    }
}
#endif

#if UNITY_ANDROID
/// <summary>
/// Silent Google Play Games authentication
/// Requires Google Play Games plugin
/// </summary>
private async System.Threading.Tasks.Task<bool> AuthenticateWithGooglePlay()
{
    var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
    
    // Using Unity's Social API (or Google Play Games Plugin)
    Social.localUser.Authenticate(success =>
    {
        if (success)
        {
            string playerId = Social.localUser.id;
            Debug.Log($"Google Play Games: Authenticated as {Social.localUser.userName}");
            
            // Link to Supabase silently
            LinkGooglePlayToSupabase(playerId).ContinueWith(task => tcs.SetResult(task.Result));
        }
        else
        {
            Debug.LogWarning("Google Play Games: Not authenticated");
            tcs.SetResult(false);
        }
    });
    
    return await tcs.Task;
}

private async System.Threading.Tasks.Task<bool> LinkGooglePlayToSupabase(string playerId)
{
    try
    {
        // Same logic as Game Center but for Google Play
        string email = $"gp_{playerId}@internal.game";
        string password = GenerateSecurePassword(playerId);
        
        // Try to sign in to existing account
        try
        {
            var existingSession = await authClient.SignIn(email, password);
            if (existingSession != null)
            {
                Debug.Log("Google Play: Signed into existing Supabase account");
                return true;
            }
        }
        catch
        {
            // Account doesn't exist
        }
        
        // Link anonymous account or create new
        if (IsAnonymousUser())
        {
            var attributes = new UserAttributes
            {
                Email = email,
                Password = password,
                Data = new Dictionary<string, object>
                {
                    { "platform", "android" },
                    { "platform_id", playerId },
                    { "display_name", Social.localUser.userName }
                }
            };
            
            var updatedUser = await authClient.Update(attributes);
            if (updatedUser != null)
            {
                Debug.Log("Google Play: Linked anonymous account");
                return true;
            }
        }
        else
        {
            var session = await authClient.SignUp(email, password, new SignUpOptions
            {
                Data = new Dictionary<string, object>
                {
                    { "platform", "android" },
                    { "platform_id", playerId },
                    { "display_name", Social.localUser.userName }
                }
            });
            
            if (session != null)
            {
                Debug.Log("Google Play: Created new Supabase account");
                return true;
            }
        }
        
        return false;
    }
    catch (Exception e)
    {
        Debug.LogError($"Google Play linking error: {e.Message}");
        return false;
    }
}
#endif

/// <summary>
/// Generate secure deterministic password from platform ID
/// IMPORTANT: Change the secret to something unique for your game!
/// </summary>
private string GenerateSecurePassword(string platformId)
{
    // This should be a secret unique to your app
    string appSecret = "MyGame_Secret_Key_" + Application.identifier;
    string combined = appSecret + platformId + "v1"; // v1 for versioning
    
    using (var sha256 = System.Security.Cryptography.SHA256.Create())
    {
        byte[] bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(combined));
        return Convert.ToBase64String(bytes);
    }
}

    private void OnDestroy()
    {
        if (supabaseClient?.Auth != null)
        {
            supabaseClient.Auth.RemoveStateChangedListener(OnAuthStateChanged);
        }
    }
}


