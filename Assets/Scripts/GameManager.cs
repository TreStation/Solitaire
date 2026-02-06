using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public GameView view;
    public Board board;
    private int score;

    private void Awake()
    {
        instance = this;
    }

    // Foundation Section

    // Handles double-clicking a card to attempt moving it to a foundation pile
    public void OnCardDoubleClicked(Card card)
    {
        foreach (Pile foundation in board.foundations)
        {
            if (TryMoveToFoundation(card, foundation))
            {
                return;
            }
        }
        OnInvalidMove();
    }

    // Attempts to move a card to a foundation pile if the move is valid
    private bool TryMoveToFoundation(Card card, Pile foundation)
    {
        if (CheckFoundationMove(card, foundation))
        {
            MoveToPile(card, foundation);
            view.AnimateCardToFoundation(card, foundation);
            CheckScore(foundation);
            card.ToggleClickable(false);
            return true;
        }
        return false;
    }

    // Validates if a card can be moved to a foundation pile
    // Aces can be placed on empty foundations
    // Other cards must match suit and be one rank higher than the top card
    public bool CheckFoundationMove(Card card, Pile foundation)
    {
        if (foundation.cards.Count == 0)
        {
            return card.rank == Rank.Ace;
        }
        else
        {
            Card topCard = foundation.cards[^1];
            return topCard.suit == card.suit && topCard.rank + 1 == card.rank;
        }
    }

    // Tableau Section
    public void HandleTableauMove(Card card, Pile targetPile)
    {
        List<Card> cardsToMove = GetStack(card);
        Card targetCard = targetPile.cards.Count > 0 ? targetPile.cards[^1] : null;

        if (targetCard != null && IsValidTableauMove(card, targetCard))
        {
            MoveCards(cardsToMove, targetPile, targetCard);
        }
        else
        {
            OnInvalidMove();
        }
    }
    private bool IsValidTableauMove(Card card, Card targetCard)
    {
        return card.color != targetCard.color && card.rank + 1 == targetCard.rank;
    }
    public void KingMove(Card card, Transform pile)
    {
        int index = ExtractIndexFromName(pile.name);
        Pile tableau = board.tableaus[index].GetComponent<Pile>();
        List<Card> cardsToMove = GetStack(card);

        MoveCards(cardsToMove, tableau, null);
        pile.gameObject.GetComponent<BoxCollider>().enabled = false;
    }

    // Card Movement Section
    public void MoveToPile(Card card, Pile targetPile)
    {
        card.parent.pile.cards.Remove(card);
        if (card.parent.type == PileType.Tableau) FlipTopCard(card.parent.pile);    
        if (card.parent.type == PileType.Stock && board.Stock.cards.Count == 0) OnStockEmpty();
        if (card.parent.type == PileType.Waste && board.stockEmpty == false) ShiftWasteUp();

        targetPile.cards.Add(card);
        card.parent = (targetPile.pileType, targetPile);
    }

    private void MoveCards(List<Card> cards, Pile targetPile, Card targetCard)
    {
        if (cards.Count > 1)
        {
            MoveStack(cards, targetPile);
        }
        else
        {
            if (targetCard != null)
            {
                view.AnimateCardToTableau(cards[0], targetCard);
            }
            else
            {
                view.AnimateKing(cards[0], targetPile);
            }
            MoveToPile(cards[0], targetPile);
        }
    }

    private void MoveStack(List<Card> stack, Pile targetPile)
    {
        Vector3 baseTarget = GetTargetPosition(targetPile);

        // Disable collision for all cards in the stack
        foreach (Card card in stack)
        {
            card.GetComponent<Collider>().enabled = false;
        }

        // Move each card in the stack to its calculated position
        for (int i = 0; i < stack.Count; i++)
        {
            Vector3 target = baseTarget;
            target.y += i * 0.005f;
            target.z += i * -0.2f;

            int index = i; // Capture the current index
            stack[index].transform.DOMoveY(0.85f, 0.5f).OnComplete(() =>
            {
                stack[index].transform.DOMove(target, 0.65f).OnComplete(() =>
                {
                    MoveToPile(stack[index], targetPile);
                    // Re-enable collision after moving
                    stack[index].GetComponent<Collider>().enabled = true;
                });
            });
        }
    }
    // Drawing Section
    public void OnStockClicked()
    {
        if (view.Animating) return;

        int cardsToDraw = GameInstance.instance.Difficulty == Difficulty.Easy? 1 : 3;

        if (board.stockEmpty) ResetStock();
        else 
        {
            int availableCards = board.Stock.cards.Count;
            int actualCardsToDraw = Mathf.Min(cardsToDraw, availableCards);
        
            // Draw the cards
            for (int i = 0; i < actualCardsToDraw; i++)
            {
                float delay = i * 0.2f;
                DOVirtual.DelayedCall(delay, () => DrawCard());
            }
        };
    }

    private void DrawCard()
    {
        Card drawnCard = board.Stock.cards[0];
        drawnCard.ToggleActive(true);
        
        int wasteCount = board.Waste.cards.Count;
        int wasteSlot = Mathf.Min(wasteCount + 1, 3);
        view.AnimateToWaste(drawnCard, wasteSlot);

        if (wasteCount >= 3) ShiftWasteDown();

        MoveToPile(drawnCard, board.Waste);
        drawnCard.ToggleClickable(true);
        if (board.Waste.cards.Count >= 2) board.Waste.cards[^2].ToggleClickable(false);
        else if (board.Waste.cards.Count >= 3) board.Waste.cards[^3].ToggleClickable(false);
        else Debug.Log("ONLY 1 CARD IN WASTE");
    }

    public void OnStockEmpty() 
    {
        view.ToggleStock(false);
        board.stockEmpty = true;
    }

    public void ResetStock() 
    {
        foreach (Card card in board.Waste.cards.ToArray())
        {
            card.ToggleActive(false);
            MoveToPile(card, board.Stock);
            card.transform.position = board.Stock.transform.position + new Vector3(0, 0.05f * board.Stock.cards.Count, 0);
        }

        board.stockEmpty = false;
        view.ToggleStock(true);
    }

    // Replenish Functions 
    private void ShiftWasteUp()
    {
        if (board.Waste.cards.Count == 0) return;
        Card topCard = board.Waste.cards[^1];
        if (board.Waste.cards.Count < 3) 
        {
            topCard.ToggleClickable(true);
            return;
        }

        Card middleCard = board.Waste.cards[^2];
        Card bottomCard = board.Waste.cards[^3];
        Debug.Log($"New Top Card: {topCard} --  New Bottom Card {bottomCard}");

        view.AnimateToWaste(topCard, 3);
        topCard.ToggleClickable(true);

        view.AnimateToWaste(middleCard, 2);

        view.AnimateToWaste(bottomCard, 1);
        bottomCard.ToggleActive(true);

    }

    private void ShiftWasteDown()
    {
        view.AnimateToWaste(board.Waste.cards[^1], 2);
        view.AnimateToWaste(board.Waste.cards[^2], 1);

        board.Waste.cards[^1].ToggleClickable(false);
        board.Waste.cards[^2].ToggleClickable(false);

        Card droppedCard = board.Waste.cards[^3];
        DOVirtual.DelayedCall(0.5f, () =>
        {
            droppedCard.transform.DOLocalMoveY(-0.5f, 0.2f);
            droppedCard.ToggleActive(false);
        });

    }

    private void FlipTopCard(Pile tableau)
    {
        if (tableau.cards.Count == 0)
        {
            tableau.col.enabled = true;
        }
        else
        {
            Card topCard = tableau.cards[^1];
            if (!topCard.Active)
            {
                view.FlipCard(topCard);
                topCard.Active = true;
            }
        }
    }

    // Gameplay Section
    private void CheckScore(Pile pile)
    {
        if (pile.cards.Count == 13) score++;
        if (score == 4) GameInstance.instance.Win(); ;
    }

    // Helper Methods
    private Vector3 GetTargetPosition(Pile targetPile)
    {
        Vector3 target = targetPile.cards.Count > 0 ? targetPile.cards[^1].transform.position : targetPile.transform.position;
        target.z += targetPile.cards.Count > 0 ? -0.2f : 0;
        target.y += 0.005f;
        return target;
    }
    
    private List<Card> GetStack(Card card)
    {
        int cardIndex = card.parent.pile.cards.FindIndex(c => c == card);
        return card.parent.pile.cards.GetRange(cardIndex, card.parent.pile.cards.Count - cardIndex);
    }

    private int ExtractIndexFromName(string name)
    {
        return name.Length > 1 && name[0] == 'T' && int.TryParse(name.Substring(1), out int index) ? index - 1 : -1;
    }

    private void OnInvalidMove()
    {
        Debug.Log("Invalid Move!");
    }

    // Debug
    public void PrintWaste()
    {
        Debug.Log($"Waste Pile contains {board.Waste.cards.Count} cards:");
        for (int i = 0; i < board.Waste.cards.Count; i++)
        {
            Card card = board.Waste.cards[i];
            Debug.Log($"  Card {i}: {card.suit} {card.rank} (Active: {card.Active})");
        }
        Debug.Log($"Stock Cards: {board.Stock.cards.Count} ------------------------");
    }
}

