/// <summary>
/// Immutable value type representing a single card's logical state.
/// Kept as a struct because it is copied into pile collections frequently.
/// </summary>
public struct CardData
{
    public SuitType Suit;
    public RankType Rank;
    public bool IsFaceUp;

    public CardData(SuitType suit, RankType rank, bool isFaceUp = false)
    {
        Suit = suit;
        Rank = rank;
        IsFaceUp = isFaceUp;
    }

}
