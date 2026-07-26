using TMPro;
using UnityEngine;

/// <summary>
/// Displays the player's persisted gem balance on a TextMeshPro label and keeps
/// it in sync with <see cref="PurchaseManager"/> via the OnGemCountChanged event.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class GemCountUI : MonoBehaviour
{
    private TMP_Text label;

    private void Awake()
    {
        label = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        if (PurchaseManager.instance != null)
        {
            PurchaseManager.instance.OnGemCountChanged += UpdateLabel;
            // Show the current balance immediately (the local cache is loaded in
            // PurchaseManager.Awake, so this is valid even offline).
            UpdateLabel(PurchaseManager.instance.GemCount);
        }
        else
        {
            UpdateLabel(0);
        }
    }

    private void OnDisable()
    {
        if (PurchaseManager.instance != null)
            PurchaseManager.instance.OnGemCountChanged -= UpdateLabel;
    }

    private void UpdateLabel(int gemCount)
    {
        if (label != null)
            label.text = gemCount.ToString();
    }
}
