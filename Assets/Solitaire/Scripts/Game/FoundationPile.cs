using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages one of the four foundation piles (one per suit).
/// All cards stack on the same world position; only the top card is fully visible.
/// </summary>
public class FoundationPile : MonoBehaviour
{
    private const float StackYOffset = 0.001f;

    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [SerializeField] public SuitType Suit;

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private readonly List<CardView> _cards = new();

    public List<CardView> Cards => _cards;

    /// <summary>True when all 13 cards of the suit have been placed.</summary>
    public bool IsComplete => _cards.Count == 13;

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Returns the top card of the foundation, or null if empty.</summary>
    public CardView GetTopCard() =>
        _cards.Count > 0 ? _cards[_cards.Count - 1] : null;

    /// <summary>Adds a card to the foundation, positions it, and disables its collider.</summary>
    public void AddCard(CardView card)
    {
        _cards.Add(card);
        card.SetFaceUp(true);
        card.SetColliderEnabled(false);
        Vector3 foundationPostion = transform.position;
        foundationPostion.y += 0.01f;
        Vector3 targetPosition = foundationPostion + Vector3.up * (_cards.Count - 1) * StackYOffset;
        card.AnimateMoveTo(targetPosition);
    }
}
