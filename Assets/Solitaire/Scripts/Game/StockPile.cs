using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the Stock and Waste piles. Handles draw-1 and draw-3 modes.
/// </summary>
public class StockPile : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    /// <summary>Small Y offset per stacked card to prevent Z-fighting.</summary>
    private const float StackYOffset = 0.002f;

    /// <summary>Horizontal spread per visible waste card in draw-3 mode (world units).</summary>
    private const float WasteSpreadX = 0.25f;

    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [SerializeField] private Transform stockTransform;
    [SerializeField] private Transform wasteTransform;

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private List<CardView> _stockCards = new();
    private List<CardView> _wasteCards = new();
    private int _drawCount = 1;

    public List<CardView> WasteCards => _wasteCards;

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Seeds the pile with the remaining cards after tableau dealing.</summary>
    public void Initialize(List<CardView> stockCards, int drawCount)
    {
        _drawCount  = drawCount;
        _stockCards = new List<CardView>(stockCards);
        _wasteCards = new List<CardView>();

        // Stack all stock cards at the stock position, face-down, collider disabled
        for (int i = 0; i < _stockCards.Count; i++)
        {
            CardView cv = _stockCards[i];
            cv.transform.position = stockTransform.position + Vector3.up * (i * StackYOffset);
            cv.SetFaceUp(false);
            cv.SetColliderEnabled(false);
        }
    }

    /// <summary>
    /// Draws up to <c>drawCount</c> cards from stock to waste.
    /// If stock is empty, resets waste back to stock.
    /// </summary>
    public void Draw()
    {
        if (_stockCards.Count == 0)
        {
            ResetWasteToStock();
            return;
        }

        int toDraw = Mathf.Min(_drawCount, _stockCards.Count);
        AudioManager.instance.PlaySfx("draw_card");
        for (int i = 0; i < toDraw; i++)
        {
            CardView cv = _stockCards[_stockCards.Count - 1];
            _stockCards.RemoveAt(_stockCards.Count - 1);
            _wasteCards.Add(cv);
            cv.SetFaceUp(true);
        }

        RefreshWastePositions();
    }

    /// <summary>Returns the top (most recently drawn) waste card, or null if waste is empty.</summary>
    public CardView GetTopWasteCard() =>
        _wasteCards.Count > 0 ? _wasteCards[_wasteCards.Count - 1] : null;

    /// <summary>Removes a specific card from the waste pile (after it has been moved elsewhere).</summary>
    public void RemoveFromWaste(CardView card)
    {
        _wasteCards.Remove(card);
        RefreshWastePositions();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void ResetWasteToStock()
    {
        // Flip all waste cards face-down and move them back to stock
        for (int i = _wasteCards.Count - 1; i >= 0; i--)
        {
            CardView cv = _wasteCards[i];
            _stockCards.Add(cv);
            cv.SetFaceUp(false);
            cv.SetColliderEnabled(false);
            cv.transform.position = stockTransform.position + Vector3.up * (_stockCards.Count - 1) * StackYOffset;
        }
        _wasteCards.Clear();
    }

    private void RefreshWastePositions()
    {
        // Disable colliders on all waste cards first
        foreach (CardView cv in _wasteCards)
        {
            cv.SetColliderEnabled(false);
        }

        if (_wasteCards.Count == 0) return;

        int visibleCount = Mathf.Min(_drawCount, _wasteCards.Count);
        int startIndex   = _wasteCards.Count - visibleCount;

        // Stack non-visible waste cards at the base waste position
        for (int i = 0; i < startIndex; i++)
        {
            _wasteCards[i].transform.position = wasteTransform.position + Vector3.up * (i * StackYOffset);
        }

        // Spread the top visible cards
        for (int i = 0; i < visibleCount; i++)
        {
            int listIndex = startIndex + i;
            float xOffset = (visibleCount == 1) ? 0f : i * WasteSpreadX;
            Vector3 pos   = wasteTransform.position
                          + Vector3.right * xOffset
                          + Vector3.up    * (listIndex * StackYOffset);
            _wasteCards[listIndex].transform.position = pos;
        }

        // Only the very top card is interactable
        CardView topCard = _wasteCards[_wasteCards.Count - 1];
        topCard.SetColliderEnabled(true);
    }
}
