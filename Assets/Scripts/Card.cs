using UnityEngine;
using DG.Tweening;

public class Card : MonoBehaviour
{
    public Suit suit;
    public Rank rank;
    public ColorType color;
    public (PileType type, Pile pile) parent;

    // Properties
    public bool Active { get; set; }

    public void Init(Suit suit, Rank rank, ColorType color, bool active)
    {
        this.suit = suit;
        this.rank = rank;
        this.color = color;
        this.Active = active;

        LoadTexture();
    }

    private void LoadTexture()
    {
        MeshRenderer mr = gameObject.GetComponentInChildren<MeshRenderer>();

        string fileName = "";
        int numericalRank = (int)rank + 1;
        string fileRank = rank switch
        {
            Rank.Ace => "A",
            Rank.Jack => "J",
            Rank.Queen => "Q",
            Rank.King => "K",
            _ => numericalRank.ToString(),
        };
        switch (suit)
        {
            case Suit.Clubs:
                fileName = $"Clubs/B_Club_{fileRank}";
                break;
            case Suit.Spades:
                fileName += $"Spades/B_Spade_{fileRank}";
                break;
            case Suit.Hearts:
                fileName += $"Hearts/R_Heart_{fileRank}";
                break;
            case Suit.Diamonds:
                fileName += $"Diamonds/R_Diamond_{fileRank}";
                break;
        }

        // Load the texture from the file
        Texture2D texture = Resources.Load<Texture2D>(fileName);
        Texture2D backTexture = Resources.Load<Texture2D>("Back/Blue_Back");

        mr.materials[1].mainTexture = texture;
        mr.materials[2].mainTexture = backTexture;
    }

    public void ToggleActive(bool active)
    {
        Active = active;
        gameObject.SetActive(active);
        transform.rotation = active ? Quaternion.Euler(0, 90, 0) : Quaternion.identity;
    }
    public void ToggleClickable(bool clickable)
    {
        if (clickable)
        {
            gameObject.layer = LayerMask.NameToLayer("Card");
        }
        else
        {
            gameObject.layer = 0;
        }
    }
}
