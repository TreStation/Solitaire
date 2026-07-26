using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks the currently selected card and the pile it came from.
/// Manages outline visibility across single-card and multi-card selections.
/// </summary>
public class SelectionManager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    public CardView SelectedCard  { get; private set; }

    /// <summary>The pile the selection originated from — a <see cref="TableauPile"/> or <see cref="StockPile"/>.</summary>
    public object SelectedSourcePile { get; private set; }

    private readonly List<CardView> _outlinedCards = new();

    public bool HasSelection => SelectedCard != null;

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Selects a card, enabling its outline and the outlines of all face-up cards
    /// above it in the same tableau column (the moveable stack).
    /// </summary>
    public void Select(CardView card, object sourcePile)
    {
        Deselect();

        SelectedCard      = card;
        SelectedSourcePile = sourcePile;
        
        if (sourcePile is TableauPile tableauPile)
        {
            List<CardView> movableStack = tableauPile.GetMovableStack(card);
            foreach (CardView cv in movableStack)
            {
                // Activate selection Outline
                cv.SetOutline(true);
                _outlinedCards.Add(cv);
                // Selection animation
                cv.PlayAnimation("Selection");
            }
            
        }
        else
        {
            card.SetOutline(true);
            _outlinedCards.Add(card);
            card.PlayAnimation("Selection");
        }
    }

    /// <summary>Clears the current selection and removes all outlines.</summary>
    public void Deselect()
    {
        foreach (CardView cv in _outlinedCards)
        {
            if (cv != null) cv.SetOutline(false);
        }
        _outlinedCards.Clear();

        SelectedCard      = null;
        SelectedSourcePile = null;
    }
}
