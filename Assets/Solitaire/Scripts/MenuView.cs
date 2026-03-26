using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuView : MonoBehaviour
{
    [Header("Menus")]
    public GameObject splashMenu;
    public GameObject loadingMenu;
    public GameObject difficultyMenu;
    public GameObject purchaseMenu;
    public GameObject purchaseCompleteMenu;
    public GameObject loadingLevelMenu;
    public GameObject drawingMenu;
    public GameObject formMenu;

    [Header("UI")]
    public TMP_Text loadingLevelText;
    public TMP_Text gemCountText;
    public TMP_InputField emailInputField;
    public Button emailNextButton;

    [Header("References")]
    public BackendManager backendManager;
    private GameObject activeMenu;

    private const float FadeInDuration = 2f;
    private const float FadeOutDuration = 1.5f;

    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", RegexOptions.Compiled);

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    private void Start()
    {
        UpdateText(gemCountText, GameInstance.instance.PlayerController.State.Gems.ToString());
        InitializeEmailInput();
    }

    private void OnDestroy() =>
        emailInputField.onValueChanged.RemoveListener(OnEmailInputChanged);

    // -------------------------------------------------------------------------
    // Navigation
    // -------------------------------------------------------------------------

    private GameObject GetMenuByName(string menuName) => menuName switch
    {
        "Splash" => splashMenu,
        "Loading" => loadingMenu,
        "Difficulty" => difficultyMenu,
        "Purchase" => purchaseMenu,
        "PurchaseComplete" => purchaseCompleteMenu,
        "Drawing" => drawingMenu,
        "Form" => formMenu,
        _ => null,
    };

    // -------------------------------------------------------------------------
    // Level loading
    // -------------------------------------------------------------------------

    public void LoadLevel(DifficultyType difficulty)
    {
        loadingLevelText.text = difficulty switch
        {
            DifficultyType.Easy => "Easy Attempt",
            DifficultyType.Hard => "Hard Attempt",
            _ => "Unknown Attempt",
        };

        GameInstance.instance.Difficulty = difficulty;
        GameInstance.instance.ShowAd();
    }

    // -------------------------------------------------------------------------
    // Raffle / gems
    // -------------------------------------------------------------------------

    private void UpdateText(TMP_Text text, string value)
    {
        text.text = value;
    }

    // -------------------------------------------------------------------------
    // Email input
    // -------------------------------------------------------------------------

    private void InitializeEmailInput()
    {
        emailInputField.onValueChanged.AddListener(OnEmailInputChanged);
        emailNextButton.interactable = false;
    }

    private void OnEmailInputChanged(string email)
    {
        bool valid = IsValidEmail(email);
        emailNextButton.interactable = valid;
        if (valid) backendManager.SetEmail(email);
    }

    public static bool IsValidEmail(string email) =>
        !string.IsNullOrWhiteSpace(email) && EmailRegex.IsMatch(email);

    // -------------------------------------------------------------------------
    // Fade helpers
    // -------------------------------------------------------------------------

    private void SetMenuActive(GameObject menu, bool isActive)
    {
        menu.SetActive(isActive);
        if (isActive) activeMenu = menu;
    }
}