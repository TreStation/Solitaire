using System;
using System.Collections.Generic;

/// <summary>
/// Builds and shuffles a standard 52-card deck.
/// Uses <see cref="System.Random"/> with an explicit seed for reproducibility.
/// </summary>
public static class DeckFactory
{
    /// <summary>
    /// Returns a shuffled list of 52 <see cref="CardData"/> using Fisher-Yates
    /// with the supplied seed. All cards start face-down.
    /// </summary>
    public static List<CardData> CreateShuffledDeck(int seed)
    {
        var deck = new List<CardData>(52);

        foreach (SuitType suit in Enum.GetValues(typeof(SuitType)))
        {
            foreach (RankType rank in Enum.GetValues(typeof(RankType)))
            {
                deck.Add(new CardData(suit, rank));
            }
        }

        var rng = new System.Random(seed);
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }

        return deck;
    }
}
