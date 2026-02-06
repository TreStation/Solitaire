using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using TMPro;

[Serializable]
public class MenuView : MonoBehaviour
{
    public GameObject splashMenu;
    public GameObject loadingMenu;
    public GameObject difficultyMenu;
    public GameObject purchaseMenu;
    public GameObject purchaseCompleteMenu;
    public GameObject activeMenu;
    public GameObject loadingLevelMenu;
    public GameObject drawingMenu;
    public GameObject formMenu;
    private readonly float fadeInDuration = 2f;
    private readonly float fadeOutDuration = 1.5f;

    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", RegexOptions.Compiled);
    
    private async void Start()
    {
        // Initialize Supabase
        await SupabaseAuthManager.Instance.InitializeSupabase();
        
        // Start anonymous (instant, works offline)
        await SupabaseAuthManager.Instance.SignInAnonymously();
        
        // Link platform account in background (silent)
        bool linked = await SupabaseAuthManager.Instance.AutoAuthenticateWithPlatform();
        
        if (linked)
        {
            Debug.Log("Platform account linked! IAPs will persist across devices.");
        }
        else
        {
            Debug.Log("Playing anonymously. Prompt user to link account before IAP.");
        }
        
        LoadMainMenu(); 
    }

    public async void OnShopClick()
    {
            if (SupabaseAuthManager.Instance.IsAnonymousUser())
            {
                // Try to link platform account
                bool linked = await SupabaseAuthManager.Instance.AutoAuthenticateWithPlatform();
            
                if (!linked)
                {
                    Debug.Log("User not linked. IAPs will not persist across devices.");
                    return;
                }
            }
            Debug.Log("NOW Process IAP");
            ShowMenu("Purchase");
    }

    private void LoadMainMenu()
    {
        FadeIn(splashMenu, () =>
        {
            FadeOut(splashMenu, () =>
            {
                FadeIn(loadingMenu, () =>
                {
                    FadeOut(loadingMenu, () =>
                    {
                        FadeIn(difficultyMenu);
                        activeMenu = difficultyMenu;
                    });
                });
            });
        });
    }
    
    private void SetMenuActive(GameObject menu, bool isActive)
    {
        menu.SetActive(isActive);
        if (isActive)
        {
            activeMenu = menu;
        }
    }

    private void FadeIn(GameObject menu, Action onComplete = null)
    {
        SetMenuActive(menu, true);
        menu.TryGetComponent<CanvasGroup>(out CanvasGroup canvasGroup);
        canvasGroup.alpha = 0;
        canvasGroup.DOFade(1, fadeInDuration).OnComplete(() => onComplete?.Invoke());
    }

    private void FadeOut(GameObject menu, Action onComplete = null)
    {
        menu.TryGetComponent<CanvasGroup>(out CanvasGroup canvasGroup);
        canvasGroup.alpha = 1;
        canvasGroup.DOFade(0, fadeOutDuration).OnComplete(() =>
        {
            SetMenuActive(menu, false);
            onComplete?.Invoke();
        });
    }

    public void OnButtonClick(string menuName)
    {
        ShowMenu(menuName);
    }

    private void ShowMenu(string menuName, Action onComplete = null)
    {
        GameObject newMenu = GetMenuByName(menuName);
        if (newMenu != null)
        {
            FadeOut(activeMenu, () => FadeIn(newMenu, onComplete));
        }
        if (newMenu == loadingLevelMenu) SceneManager.LoadScene("Game");
    }

    private GameObject GetMenuByName(string menuName)
    {
        return menuName switch
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
    }

    public void LoadLevel(string levelName)
    {
        string levelText;
        Difficulty difficulty;
        if (levelName == "Easy")
        {
            levelText = "Easy Attempt";
            difficulty = Difficulty.Easy;
        }
        else if (levelName == "Hard")
        {
            levelText = "Hard Attempt";
            difficulty = Difficulty.Hard;
        }
        else
        {
            levelText = "Null Attempt";
            difficulty = Difficulty.Easy;
        }
        GameInstance.instance.Difficulty = difficulty;
        SceneManager.LoadScene("Game");
    }

}