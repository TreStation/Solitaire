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
/// Supports Email/Password (or Username/Password) sign-up and login.
/// </summary>
public class SupabaseAuthManager : MonoBehaviour
{
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

    private async void Start()
    {
        await InitializeSupabase();
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
    /// Sign up a new user with email and password
    /// </summary>
    public async System.Threading.Tasks.Task<bool> SignUp(string email, string password, string displayName = null)
    {
        try
        {
            var options = new SignUpOptions();
            
            // Store display name if provided
            if (!string.IsNullOrEmpty(displayName))
            {
                options.Data = new Dictionary<string, object>
                {
                    { "display_name", displayName }
                };
            }

            var session = await authClient.SignUp(email, password, options);
            
            if (session != null && session.User != null)
            {
                Debug.Log($"Sign up successful: {session.User.Email}");
                return true;
            }
            
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"Sign up error: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sign in with email and password
    /// </summary>
    public async System.Threading.Tasks.Task<bool> SignIn(string email, string password)
    {
        try
        {
            var session = await authClient.SignIn(email, password);
            
            if (session != null && session.User != null)
            {
                Debug.Log($"Sign in successful: {session.User.Email}");
                return true;
            }
            
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"Sign in error: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sign out the current user
    /// </summary>
    public async System.Threading.Tasks.Task SignOut()
    {
        try
        {
            await authClient.SignOut();
            Debug.Log("Signed out successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Sign out error: {e.Message}");
        }
    }

    /// <summary>
    /// Check if user is currently authenticated
    /// </summary>
    public bool IsUserAuthenticated()
    {
        return authClient?.CurrentUser != null;
    }

    /// <summary>
    /// Get current user object
    /// </summary>
    public User GetCurrentUser()
    {
        return authClient?.CurrentUser;
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
                break;
            case Constants.AuthState.SignedOut:
                Debug.Log("User signed out");
                break;
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
