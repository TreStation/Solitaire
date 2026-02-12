using UnityEngine;
using UnityEngine.UIElements;
using System;

public class LoginNavigationController : MonoBehaviour
{
    [Header("UI Document")]
    [SerializeField] private UIDocument uiDocument;
    public GameObject menuView;

    // Screens
    private VisualElement _root;
    private VisualElement _loginScreen;
    private VisualElement _addProfilePicScreen;
    private VisualElement _createPasswordScreen;

    // Login Screen Elements
    private TextField _loginUsernameInput;
    private TextField _loginPasswordInput;
    private Button _signInBtn;
    private Button _createAccountBtn;

    // Add Profile Pic Screen Elements
    private Button _profileBackBtn;
    private Button _profileNextBtn;
    private TextField _profileUsernameInput; 

    // Create Password Screen Elements
    private Button _passwordBackBtn;
    private TextField _createPasswordInput;
    private TextField _confirmPasswordInput;
    private Button _passwordNextBtn; // This acts as the "Finish" or "Create" button

    // Temporary storage for sign-up flow
    private string _pendingUsername;

    private void OnEnable()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogError("LoginNavigationController: No UIDocument assigned or found on GameObject.");
            return;
        }

        _root = uiDocument.rootVisualElement;
        if (_root == null) return;

        // 1. Query Screens (Instances in Root.uxml)
        _loginScreen = _root.Q<VisualElement>("LoginScreen");
        _addProfilePicScreen = _root.Q<VisualElement>("AddProfilePicScreen");
        _createPasswordScreen = _root.Q<VisualElement>("CreatePasswordScreen");

        // 2. Setup Logic for each screen
        SetupLoginScreen();
        SetupAddProfilePicScreen();
        SetupCreatePasswordScreen();
    }

    private void SetupLoginScreen()
    {
        if (_loginScreen == null) return;

        _loginUsernameInput = _loginScreen.Q<TextField>("username-input");
        _loginPasswordInput = _loginScreen.Q<TextField>("password-input");
        _signInBtn = _loginScreen.Q<Button>("sign-in-btn");
        _createAccountBtn = _loginScreen.Q<Button>("create-account-btn");

        if (_signInBtn != null)
            _signInBtn.clicked += OnSignInClicked;

        if (_createAccountBtn != null)
            _createAccountBtn.clicked += OnCreateAccountClicked;
    }

    private void SetupAddProfilePicScreen()
    {
        if (_addProfilePicScreen == null) return;

        _profileBackBtn = _addProfilePicScreen.Q<Button>("back-btn");
        _profileNextBtn = _addProfilePicScreen.Q<Button>("next-btn");
        _profileUsernameInput = _addProfilePicScreen.Q<TextField>("username-input");

        if (_profileBackBtn != null)
            _profileBackBtn.clicked += OnProfileBackClicked;

        if (_profileNextBtn != null)
            _profileNextBtn.clicked += OnProfileNextClicked;
    }

    private void SetupCreatePasswordScreen()
    {
        if (_createPasswordScreen == null) return;

        _passwordBackBtn = _createPasswordScreen.Q<Button>("back-btn");
        _createPasswordInput = _createPasswordScreen.Q<TextField>("password-input");
        _confirmPasswordInput = _createPasswordScreen.Q<TextField>("confirm-password-input");
        _passwordNextBtn = _createPasswordScreen.Q<Button>("next-btn");

        if (_passwordBackBtn != null)
            _passwordBackBtn.clicked += OnPasswordBackClicked;

        if (_passwordNextBtn != null)
            _passwordNextBtn.clicked += OnPasswordNextClicked;
    }

    // --- Event Handlers ---

    // Login Screen Events
    private async void OnSignInClicked()
    {
        string username = _loginUsernameInput?.value;
        string password = _loginPasswordInput?.value;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Debug.LogWarning("Login Failed: Username or Password cannot be empty.");
            return;
        }

        // Convert username to email if it's not already an email
        string email = GetEmailFromUsername(username);
        
        Debug.Log($"Attempting to sign in as: {email}");
        
        bool success = await SupabaseAuthManager.Instance.SignIn(email, password);
        
        if (success)
        {
            Debug.Log($"Login Successful! User: {username}");
            ShowMainMenu();
        }
        else
        {
            Debug.LogWarning("Login Failed. Check credentials.");
        }
    }

    private void ShowMainMenu()
    {
        menuView.SetActive(true);
    }

    private void OnCreateAccountClicked()
    {
        ShowScreen(_addProfilePicScreen);
    }

    // Add Profile Pic Screen Events
    private void OnProfileBackClicked()
    {
        ShowScreen(_loginScreen);
    }

    private void OnProfileNextClicked()
    {
        _pendingUsername = _profileUsernameInput?.value;

        if (string.IsNullOrEmpty(_pendingUsername))
        {
            Debug.LogWarning("Username cannot be empty");
            return;
        }

        // Navigate to Create Password Screen
        ShowScreen(_createPasswordScreen);
    }

    // Create Password Screen Events
    private void OnPasswordBackClicked()
    {
        ShowScreen(_addProfilePicScreen);
    }

    private async void OnPasswordNextClicked()
    {
        string pass = _createPasswordInput?.value;
        string confirmPass = _confirmPasswordInput?.value;

        if (string.IsNullOrEmpty(pass))
        {
            Debug.LogWarning("Password creation failed: Password cannot be empty.");
            return;
        }

        if (pass != confirmPass)
        {
            Debug.LogWarning("Password creation failed: Passwords do not match.");
            return;
        }

        // Proceed to create account
        string email = GetEmailFromUsername(_pendingUsername);
        
        Debug.Log($"Creating account for: {email}");
        
        bool success = await SupabaseAuthManager.Instance.SignUp(email, pass, _pendingUsername);

        if (success)
        {
            Debug.Log("Account Created Successfully! You are now logged in.");
            // Optionally auto-navigate to the game or back to login if email confirmation is required
            if (SupabaseAuthManager.Instance.IsUserAuthenticated())
            {
                ShowMainMenu();
            }
            else
            {
                // Go back to login screen
                ShowScreen(_loginScreen);
            }
        }
        else
        {
            Debug.LogWarning("Account Creation Failed.");
        }
    }

    // --- Helper ---

    private string GetEmailFromUsername(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        // If it looks like an email, use it. Otherwise, append domain.
        if (input.Contains("@")) return input;
        
        // Use a consistent domain for username-based logins
        return $"{input}@solitaire.game";
    }

    private void ShowScreen(VisualElement screenToShow)
    {
        // Hide all screens first
        HideAllScreens();

        // Show the target screen
        if (screenToShow != null)
        {
            screenToShow.style.display = DisplayStyle.Flex;
        }
    }

    private void HideAllScreens()
    {
        if (_loginScreen != null) _loginScreen.style.display = DisplayStyle.None;
        if (_addProfilePicScreen != null) _addProfilePicScreen.style.display = DisplayStyle.None;
        if (_createPasswordScreen != null) _createPasswordScreen.style.display = DisplayStyle.None;
        
        // Hide other potential screens
        VisualElement phoneScreen = _root.Q<VisualElement>("PhoneNumberScreen");
        if (phoneScreen != null) phoneScreen.style.display = DisplayStyle.None;
        
        VisualElement verifyScreen = _root.Q<VisualElement>("VerifyCodeScreen");
        if (verifyScreen != null) verifyScreen.style.display = DisplayStyle.None;
    }

    private void OnDisable()
    {
        // Unsubscribe to avoid memory leaks if object is destroyed
        if (_signInBtn != null) _signInBtn.clicked -= OnSignInClicked;
        if (_createAccountBtn != null) _createAccountBtn.clicked -= OnCreateAccountClicked;
        
        if (_profileBackBtn != null) _profileBackBtn.clicked -= OnProfileBackClicked;
        if (_profileNextBtn != null) _profileNextBtn.clicked -= OnProfileNextClicked;

        if (_passwordBackBtn != null) _passwordBackBtn.clicked -= OnPasswordBackClicked;
        if (_passwordNextBtn != null) _passwordNextBtn.clicked -= OnPasswordNextClicked;
    }
}
