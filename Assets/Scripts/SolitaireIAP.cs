using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;
using TMPro;

public class SolitaireIAP : MonoBehaviour
{
    public Button premiumButton;
    public Button gemBoostButton;
    public TMP_Text premiumBtnText;
    public TMP_Text gemBoostText;

    private void Awake()
    {
        if (PlayerPrefs.GetInt("Premium", 0) == 1)
        {
            premiumButton.interactable = false;
            premiumBtnText.text = "Purchased";
        }
        if (PlayerPrefs.GetInt("GemBoost", 0) == 1)
        {
            gemBoostButton.interactable = false;
            gemBoostText.text = "Purchased";
        }
    }

    public void Purchased(Product product)
    {
        if (product.definition.id.Equals("premiumiap"))
        {
            PlayerPrefs.SetInt("Premium", 1);
            PlayerPrefs.Save();
            Debug.Log("Premium Purchased");

            premiumButton.interactable = false; // Disable the premium button
            premiumBtnText.text = "Purchased";
        }
        else if (product.definition.id.Equals("gemboostIAP"))
        {
            PlayerPrefs.SetInt("GemBoost", 1);
            PlayerPrefs.Save();
            Debug.Log("Gem Boost Purchased");

            gemBoostButton.interactable = false;
            gemBoostText.text = "Purchased";
        }
    }

    public void OnRestoredPurchase()
    {
        Debug.Log("Restore Purchase");
    }
}
