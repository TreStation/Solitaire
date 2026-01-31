using System;
using System.Collections.Generic;
using UnityEngine;


public struct CardData
{
    public Suit suit;
    public Rank rank;
    public ColorType color;
}

public class Deck : MonoBehaviour
{
    public List<CardData> CardModels { get; set; }
    public void Create()
    {
        CardModels = new();

        for (int suit = 0; suit < 4; suit++)
        {
            for (int rank = 0; rank < 13; rank++)
            {
                CardData cardModel = new()
                {
                    suit = (Suit)suit,
                    rank = (Rank)rank,
                    color = ((Suit)suit == Suit.Clubs || (Suit)suit == Suit.Spades) ? ColorType.Black : ColorType.Red
                };
                CardModels.Add(cardModel);
            }
        };
        Shuffle();
    }

    private void Shuffle()
    {
        int n = CardModels.Count;
        System.Random random = new();

        for (int i = n - 1; i > 0; i--)
        {
            int j = random.Next(0, i + 1);
            (CardModels[j], CardModels[i]) = (CardModels[i], CardModels[j]);
        }
    }
    public bool IsEmpty()
    {
        return CardModels.Count < 1;
    }

    public CardData Draw()
    {
        // Draw the top card
        CardData drawnCardModel = CardModels[0];
        CardModels.Remove(drawnCardModel);

        // Return the drawn card
        return drawnCardModel;
    }
}
