using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages one of the seven tableau columns. Handles card stacking,
/// face-up/down state, and position layout along the Z-axis.
/// </summary>
public class TableauPile : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [SerializeField] private Transform columnRoot;
    [SerializeField] private TableauConfig config;
    [SerializeField] private Collider _collider;

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private readonly List<CardView> _cards = new();

    public List<CardView> Cards => _cards;

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Sets the initial deal for this column. All cards start face-down,
    /// then the accessible bottom card is revealed via <see cref="RevealTopCard"/>.
    /// </summary>
    public void Initialize(List<CardView> cards)
    {
        _cards.Clear();
        foreach (CardView cv in cards)
        {
            cv.SetFaceUp(false);
            _cards.Add(cv);
        }

        RevealTopCard();
        RefreshPositions();
    }

    /// <summary>Returns the topmost (last) card, or null if the column is empty.</summary>
    public CardView GetTopCard() =>
        _cards.Count > 0 ? _cards[_cards.Count - 1] : null;

    /// <summary>
    /// Flips the accessible bottom card of the cascade face-up.
    /// Call this after the initial deal and after any successful card move
    /// to expose the next playable card.
    /// </summary>
    public void RevealTopCard()
    {
        CardView top = GetTopCard();
        if (top != null && !top.Data.IsFaceUp)
        {
            top.SetFaceUp(true);
            _collider.enabled = false;
        }
        else
        {
            // Enable tableau collider interaction when tableau is empty
            _collider.enabled = true;
        }
    }

    /// <summary>
    /// Returns all face-up cards from <paramref name="fromCard"/> to the top of the column
    /// (the moveable stack when dragging a group).
    /// </summary>
    public List<CardView> GetMovableStack(CardView fromCard)
    {
        int index = _cards.IndexOf(fromCard);
        if (index < 0) return new List<CardView>();

        var stack = new List<CardView>();
        for (int i = index; i < _cards.Count; i++)
        {
            if (!_cards[i].Data.IsFaceUp) break;
            stack.Add(_cards[i]);
        }
        return stack;
    }

    /// <summary>Adds a stack of cards to the top of this column and repositions everything.</summary>
    public void AddCards(List<CardView> cards)
    {
        int firstNewIndex = _cards.Count;
        foreach (CardView cv in cards)
        {
            _cards.Add(cv);
        }
        // Newly added cards fly to their slot; existing cards snap (they don't move).
        RefreshPositions(firstNewIndex);
        AudioManager.instance.PlaySfx("valid_move");
        _collider.enabled = false;
    }

    /// <summary>
    /// Removes cards from <paramref name="fromCard"/> (inclusive) to the top of the column.
    /// Flips the new top card face-up if it was face-down.
    /// Returns the removed stack.
    /// </summary>
    public List<CardView> RemoveCards(CardView fromCard)
    {
        int index = _cards.IndexOf(fromCard);
        if (index < 0) return new List<CardView>();

        var removed = _cards.GetRange(index, _cards.Count - index);
        _cards.RemoveRange(index, _cards.Count - index);

        // Expose the new bottom card
        RevealTopCard();
        RefreshPositions();
        return removed;
    }

    /// <summary>
    /// Repositions all cards in the column using the column root's local space.
    /// Cards at index &gt;= <paramref name="animateFromIndex"/> fly to their slot via a
    /// magnetic arc; the rest snap instantly. Pass -1 (default) to snap everything.
    /// </summary>
    public void RefreshPositions(int animateFromIndex = -1)
    {
        if (columnRoot == null)
        {
            Debug.LogWarning($"[TableauPile] columnRoot is null on {gameObject.name}");
            return;
        }

        if (config == null)
        {
            Debug.LogWarning($"[TableauPile] config is null on {gameObject.name}");
            return;
        }

        float cascadeOffset = 0f;
        for (int i = 0; i < _cards.Count; i++)
        {
            Vector3 localOffset = new Vector3(-cascadeOffset, 0f, i * config.stackZOffset);
            Vector3 worldPos = columnRoot.TransformPoint(localOffset);

            if (animateFromIndex >= 0 && i >= animateFromIndex)
            {
                _cards[i].AnimateMoveTo(worldPos);
            }
            else
            {
                _cards[i].transform.position = worldPos;
            }

            float step = _cards[i].Data.IsFaceUp ? Mathf.Abs(config.faceUpOffset) : Mathf.Abs(config.faceDownOffset);
            cascadeOffset += step;
        }

        Debug.Log($"[TableauPile] {gameObject.name} positioned {_cards.Count} cards from {columnRoot.position}");
    }
}
