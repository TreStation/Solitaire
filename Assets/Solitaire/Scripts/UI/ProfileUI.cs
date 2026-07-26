using TMPro;
using UnityEngine;

public class ProfileUI : MonoBehaviour
{
    [SerializeField] private TMP_Text usernameUI;
    private async void OnEnable()
    {
        if (AuthManager.instance == null)
        {
            usernameUI.text = string.Empty;
            return;
        }

        bool hasPlayerInfo = await AuthManager.instance.InitializeAndRefreshPlayerInfoAsync();
        usernameUI.text = hasPlayerInfo && AuthManager.instance.HasAccountLinked
            ? AuthManager.instance.CurrentUsername
            : string.Empty;
    }
}
