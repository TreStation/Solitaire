using UnityEngine;

/// <summary>
/// Stateless helper with all Klondike Solitaire move-legality rules.
/// </summary>
public static class MoveValidator
{
    /// <summary>
    /// Returns true if <paramref name="card"/> can be placed on top of
    /// <paramref name="topOfPile"/> in a tableau column.
    /// </summary>
    /// <param name="card">The card being moved.</param>
    /// <param name="topOfPile">Current top card of the target column; null = empty column.</param>
    public static bool CanPlaceOnTableau(CardData card, CardData? topOfPile)
    {
        if (topOfPile == null)
        {
            Debug.Log("CAN PLACE ON TAB: " + card.Rank + RankType.King);
            return card.Rank == RankType.King;
        }

        CardData top = topOfPile.Value;
        bool alternatingColor = IsRed(card.Suit) != IsRed(top.Suit);
        bool descendingRank = (int)card.Rank == (int)top.Rank - 1;

        bool validMove = alternatingColor && descendingRank;
        if (validMove)
        {
            return true;
        }
        else return false;
    }

    /// <summary>
    /// Returns true if <paramref name="card"/> can be placed on the specified foundation pile.
    /// </summary>
    /// <param name="card">The card being moved.</param>
    /// <param name="topOfFoundation">Current top card of the foundation; null = empty pile.</param>
    /// <param name="foundationSuit">The suit this foundation pile accepts.</param>
    public static bool CanPlaceOnFoundation(CardData card, CardData? topOfFoundation, SuitType foundationSuit)
    {
        if (card.Suit != foundationSuit)
        {
            return false;
        }

        if (topOfFoundation == null)
        {
            return card.Rank == RankType.Ace;
        }

        bool validMove = (int)card.Rank == (int)topOfFoundation.Value.Rank + 1;
        if (validMove)
        {
            return true;
        }
        else return false;
    }

    private static bool IsRed(SuitType suit) =>
        suit == SuitType.Hearts || suit == SuitType.Diamonds;
}
