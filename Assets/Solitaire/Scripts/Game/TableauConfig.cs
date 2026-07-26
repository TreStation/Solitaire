using UnityEngine;

/// <summary>
/// Shared configuration for all tableau pile columns.
/// Create one instance via Assets > Create > Solitaire > Tableau Config
/// and assign it to every TableauPile component.
/// </summary>
[CreateAssetMenu(menuName = "Solitaire/Tableau Config", fileName = "TableauConfig")]
public class TableauConfig : ScriptableObject
{
    [Tooltip("Local-space cascade step for face-down cards.")]
    public float faceDownOffset = 0.4f;

    [Tooltip("Local-space cascade step for face-up cards.")]
    public float faceUpOffset = 0.5f;

    [Tooltip("Small Z increment per card to prevent Z-fighting.")]
    public float stackZOffset = 0.001f;
}
