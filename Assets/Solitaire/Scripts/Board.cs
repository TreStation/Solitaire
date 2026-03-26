using System;
using UnityEngine;

public class Board : MonoBehaviour
{
    public GameObject cardPrefab; // Array of card prefabs
    public GameObject[] tableaus; // Array of parent GameObjects for each tableau
    public Pile[] foundations; // Array of parent GameObjects for each foundation
    public Pile Waste;
    public Pile Stock;
    public bool stockEmpty = false;

    // Private variables
    private Deck deck;

    private void Awake()
    {
        deck = gameObject.AddComponent<Deck>();
        deck.Create();
    }

    void Start()
    {
        CreateBoard();
    }

    private void CreateBoard()
    {
        foreach (GameObject tableau in tableaus)
        {
            SpawnTableau(tableau);
        }
        CreateStock();
    }

    private void SpawnTableau(GameObject tableauObject)
    {
        Pile tableau = tableauObject.GetComponent<Pile>();
        int lastChildIndex = tableauObject.transform.childCount - 1;

        for (int i = 0; i <= lastChildIndex; i++)
        {
            Transform child = tableauObject.transform.GetChild(i);
            CardData drawnCard = deck.Draw();
            bool isActive = i == lastChildIndex;

            Card card = InstantiateCard(child.position, isActive ? Quaternion.Euler(0, 90, 0) : cardPrefab.transform.rotation, drawnCard, isActive);
            card.parent = (PileType.Tableau, tableau);
            card.gameObject.name = $"{card.suit}_{card.rank}_{tableau.name}_{child.name}";
            tableau.cards.Add(card);
        }
    }

    private void CreateStock()
    {
        float spawnOffset = 0.05f;
        Vector3 spawnPos = Stock.transform.position;

        foreach (CardData drawnCard in deck.CardModels)
        {
            spawnPos.y -= spawnOffset;
            Card card = InstantiateCard(spawnPos, Quaternion.Euler(0, 90, 0), drawnCard, false);
            card.parent = (PileType.Stock, Stock);
            card.gameObject.name = $"{card.suit}_{card.rank}_{Stock.name}";
            Stock.cards.Add(card);
        }
    }

    private Card InstantiateCard(Vector3 position, Quaternion rotation, CardData cardData, bool isActive)
    {
        Card card = Instantiate(cardPrefab, position, rotation).GetComponent<Card>();
        card.Init(cardData.suit, cardData.rank, cardData.color, isActive);
        return card;
    }
}

