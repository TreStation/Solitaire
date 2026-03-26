using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;
using TMPro;

public class SolitaireIAP : MonoBehaviour
{
    public Button premiumButton;
    public TMP_Text premiumBtnText;
    
    public Button gemBoostButton;
    public TMP_Text gemBoostText;

    private const string premiumIap = "premiumiap";
    private const string gemBoostIap = "gemboostiap";

    private void Awake()
    {
        bool premiumActive = GameInstance.PlayerController.State.Premium;
        bool gemBoostActive = GameInstance.PlayerController.State.GemBoost;

        ToggleButtonActive(premiumActive, premiumButton, premiumBtnText);
        ToggleButtonActive(gemBoostActive, gemBoostButton, gemBoostText);
    }

    private void ToggleButtonActive(bool active, Button button, TMP_Text text)
    {
        button.interactable = !active;
        text.text = active ? "Purchased" : "Buy";
    }

    public void Purchased(Product product)
    {
        if (product.definition.id.Equals(premiumIap))
        {
            GameInstance.PlayerController.State.Premium = true;
            GameInstance.SaveToDevice();

            ToggleButtonActive(true, premiumButton, premiumBtnText);
        }
        else if (product.definition.id.Equals(gemBoostIap))
        {
            GameInstance.PlayerController.State.GemBoost = true;
            GameInstance.SaveToDevice();

            ToggleButtonActive(true, gemBoostButton, gemBoostText);
        }
    }

    public void OnRestoredPurchase()
    {
        // #TODO: Implement restore purchase functionality
        Debug.Log("Restore Purchase");
    }
}
