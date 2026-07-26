using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central orchestrator for a Klondike Solitaire session.
/// Deals cards, routes input events, executes moves, and checks for win.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [SerializeField] private InputHandler      inputHandler;
    [SerializeField] private SelectionManager  selectionManager;
    [SerializeField] private StockPile         stockPile;
    [SerializeField] private FoundationPile[]  foundationPiles;   // length 4, ordered Clubs/Spades/Hearts/Diamonds
    [SerializeField] private TableauPile[]     tableauPiles;      // length 7
    [SerializeField] private GameObject        cardPrefab;
    [SerializeField] private MovePopup         movePopup;         // "Nice!" / "Perfect!" feedback badge
	[SerializeField] private GameObject        winScreen;
    [SerializeField] private GameObject gemBoostUI;
    [SerializeField] private CardArtType cardArt = CardArtType.Palm;

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private readonly List<CardView> _allCards = new();

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void Start()
    {
        
        // DEBUG CARD BACK
        GameInstance.instance.CardArt = cardArt;
        
        
        int drawCount = GetDrawCount();
        int seed      = GetOrGenerateSeed();

        List<CardData> deck = DeckFactory.CreateShuffledDeck(seed);

        InstantiateCards(deck);
        DealCards(drawCount);
        SubscribeToInput();

        Debug.Log($"[GameManager] Game started — seed={seed}, drawCount={drawCount}");
    }

    private void OnDestroy()
    {
        if (inputHandler == null) return;
        inputHandler.OnSingleClick.RemoveListener(OnSingleClick);
        inputHandler.OnDoubleClick.RemoveListener(OnDoubleClick);
        inputHandler.OnMissClick.RemoveListener(OnMissClick);
    }

    // -------------------------------------------------------------------------
    // Initialisation helpers
    // -------------------------------------------------------------------------

    private int GetDrawCount()
    {
        if (GameInstance.instance == null) return 1;
        return GameInstance.instance.Difficulty == DifficultyType.Hard ? 3 : 1;
    }

    private int GetOrGenerateSeed()
    {
        if (GameInstance.instance == null)
        {
            return Environment.TickCount;
        }

        if (GameInstance.instance.GameSeed == 0)
        {
            GameInstance.instance.GameSeed = Environment.TickCount;
        }

        return GameInstance.instance.GameSeed;
    }

    private void InstantiateCards(List<CardData> deck)
    {
        _allCards.Clear();
        foreach (CardData data in deck)
        {
            GameObject go = Instantiate(cardPrefab);
            CardView cv   = go.GetComponent<CardView>();
            cv.Initialize(data);
            _allCards.Add(cv);
        }
    }

    private void DealCards(int drawCount)
    {
        int cardIndex = 0;

        // Deal tableau: column i gets (i + 1) cards
        for (int col = 0; col < tableauPiles.Length; col++)
        {
            int count = col + 1;
            var columnCards = new List<CardView>();

            for (int c = 0; c < count; c++)
            {
                columnCards.Add(_allCards[cardIndex++]);
            }

            tableauPiles[col].Initialize(columnCards);
        }

        // Remaining cards go to stock
        var stockCards = new List<CardView>();
        for (int i = cardIndex; i < _allCards.Count; i++)
        {
            stockCards.Add(_allCards[i]);
        }

        stockPile.Initialize(stockCards, drawCount);
    }

    private void SubscribeToInput()
    {
        inputHandler.OnSingleClick.AddListener(OnSingleClick);
        inputHandler.OnDoubleClick.AddListener(OnDoubleClick);
        inputHandler.OnMissClick.AddListener(OnMissClick);
    }

    // -------------------------------------------------------------------------
    // Input routing
    // -------------------------------------------------------------------------

    private void OnSingleClick(Transform hit)
    {
        Debug.Log($"[SingleClick] Hit: {hit.name} | HasSelection: {selectionManager.HasSelection}");

        // Stock area tapped → draw
        if (hit.gameObject == stockPile.gameObject)
        {
            selectionManager.Deselect();
            stockPile.Draw();
            AudioManager.instance.PlaySfx("draw_card");
            return;
        }

        // Foundation area tapped with an active selection → attempt move
        FoundationPile foundationHit = hit.GetComponentInParent<FoundationPile>();
        if (foundationHit != null && selectionManager.HasSelection)
        {
            TryMoveToFoundation(foundationHit);
            return;
        }

        CardView cardHit = hit.GetComponentInParent<CardView>();
        TableauPile tableauHit = hit.GetComponentInParent<TableauPile>();
        Debug.Log($"[SingleClick] cardHit={cardHit?.name ?? "null"} | tableauHit={tableauHit?.name ?? "null"}");

        // Empty tableau column tapped with an active selection → attempt move (e.g. King to empty column)
        if (cardHit == null)
        {
            if (tableauHit != null && tableauHit.GetTopCard() == null && selectionManager.HasSelection)
            {
                TryMoveSelectedToEmptyTableau(tableauHit);
            }
            return;
        }

        if (selectionManager.HasSelection)
        {
            if (cardHit == selectionManager.SelectedCard)
            {
                // Second single-click on the same selected card: treat as an auto-move attempt.
                // This handles slow double-clicks that exceed DoubleClickThreshold and register
                // as two sequential single-clicks instead.
                CardView toAutoMove = selectionManager.SelectedCard;
                selectionManager.Deselect();
                TryAutoMoveToFoundation(toAutoMove);
                return;
            }

            // Check if the tapped card is itself in a pile → attempt move
            bool moved = TryMoveSelectedTo(cardHit);
            if (!moved)
            {
                // Invalid target — reselect the new card instead
                TrySelectCard(cardHit);
            }
        }
        else
        {
            TrySelectCard(cardHit);
        }
    }

    private void OnDoubleClick(Transform hit)
    {
        CardView cv = hit.GetComponentInParent<CardView>();
        if (cv == null || !cv.Data.IsFaceUp) return;

        // Deselect first so auto-move logic is clean
        selectionManager.Deselect();
        TryAutoMoveToFoundation(cv);
    }

    private void OnMissClick()
    {
        selectionManager.Deselect();
    }

    // -------------------------------------------------------------------------
    // Selection
    // -------------------------------------------------------------------------

    private void TrySelectCard(CardView card)
    {
        if (!card.Data.IsFaceUp) return;

        object pile = FindPileForCard(card);
        if (pile == null) return;

        // Waste: only the top card is selectable
        if (pile is StockPile sp && card != sp.GetTopWasteCard()) return;

        selectionManager.Select(card, pile);
    }

    // -------------------------------------------------------------------------
    // Move execution
    // -------------------------------------------------------------------------

    /// <summary>Tries to move the selected card/stack onto <paramref name="targetCard"/>'s pile.</summary>
    private bool TryMoveSelectedTo(CardView targetCard)
    {
        CardView     selected   = selectionManager.SelectedCard;
        object       sourcePile = selectionManager.SelectedSourcePile;
        CardData     selData    = selected.Data;

        object targetPile = FindPileForCard(targetCard);
        if (targetPile == null) return false;

        // ── Tableau → Tableau ───────────────────────────────────────────────
        if (targetPile is TableauPile targetTab)
        {
            if (!targetCard.Data.IsFaceUp) return false;

            CardData? topOfTarget = targetCard.Data;
            if (!MoveValidator.CanPlaceOnTableau(selData, topOfTarget)) return false;

            List<CardView> stack = ExtractStack(selected, sourcePile);
            if (stack == null) return false;

            targetTab.AddCards(stack);
            
            // Effects
            movePopup?.Show(false, selected.transform.position);
            AudioManager.instance.PlaySfx("valid_move");
            
            selectionManager.Deselect();
            CheckWin();
            return true;
        }

        // ── Tableau → Foundation (via card on foundation) ────────────────────
        if (targetPile is FoundationPile targetFnd)
        {
            return TryMoveToFoundation(targetFnd);
        }

        return false;
    }

    /// <summary>Tries to move the selected card/stack onto an empty tableau column (only a King is legal).</summary>
    private bool TryMoveSelectedToEmptyTableau(TableauPile targetTab)
    {
        CardView selected   = selectionManager.SelectedCard;
        object   sourcePile = selectionManager.SelectedSourcePile;
        if (selected == null) return false;

        // topOfPile == null → MoveValidator only allows a King here.
        if (!MoveValidator.CanPlaceOnTableau(selected.Data, null)) return false;

        List<CardView> stack = ExtractStack(selected, sourcePile);
        if (stack == null) return false;

        targetTab.AddCards(stack);
        movePopup?.Show(false, selected.transform.position);
        AudioManager.instance.PlaySfx("valid_move");
        selectionManager.Deselect();
        CheckWin();
        return true;
    }

    private bool TryMoveToFoundation(FoundationPile targetFoundation)
    {
        CardView selected   = selectionManager.SelectedCard;
        object   sourcePile = selectionManager.SelectedSourcePile;

        if (selected == null) return false;

        CardData? topOfFoundation = targetFoundation.GetTopCard()?.Data;
        if (!MoveValidator.CanPlaceOnFoundation(selected.Data, topOfFoundation, targetFoundation.Suit))
        {
            return false;
        }

        // Only single cards can go to foundations (no stack move)
        List<CardView> stack = ExtractStack(selected, sourcePile);
        if (stack == null || stack.Count != 1) return false;

        targetFoundation.AddCard(selected);
        movePopup?.Show(true, selected.transform.position);
        AudioManager.instance.PlaySfx("valid_move");
        selectionManager.Deselect();
        CheckWin();
        return true;
    }

    private void TryAutoMoveToFoundation(CardView card)
    {
        object sourcePile = FindPileForCard(card);
        if (sourcePile == null)
        {
            Debug.LogWarning($"[AutoMove] Source pile not found for {card.Data.Suit} {card.Data.Rank} — card may already be on a foundation or is missing from all piles.");
            return;
        }

        Debug.Log($"[AutoMove] Attempting auto-move: {card.Data.Suit} {card.Data.Rank} from {sourcePile.GetType().Name}");

        foreach (FoundationPile fp in foundationPiles)
        {
            CardData? top = fp.GetTopCard()?.Data;
            bool canPlace = MoveValidator.CanPlaceOnFoundation(card.Data, top, fp.Suit);
            Debug.Log($"[AutoMove] → fp.Suit={fp.Suit} top={top?.Rank.ToString() ?? "empty"} canPlace={canPlace}");

            if (!canPlace) continue;

            List<CardView> stack = ExtractStack(card, sourcePile);
            if (stack == null || stack.Count != 1)
            {
                Debug.LogWarning($"[AutoMove] ExtractStack failed for {card.Data.Suit} {card.Data.Rank}: count={stack?.Count.ToString() ?? "null"} sourcePile={sourcePile.GetType().Name}");
                return;
            }

            fp.AddCard(card);
            movePopup?.Show(true, card.transform.position);
            AudioManager.instance.PlaySfx("valid_move");
            Debug.Log($"[AutoMove] Moved {card.Data.Suit} {card.Data.Rank} to {fp.Suit} foundation.");
            CheckWin();
            return;
        }

        Debug.Log($"[AutoMove] No valid foundation for {card.Data.Suit} {card.Data.Rank}.");
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Removes the card (and its moveable stack if from a tableau pile) from its source pile.
    /// Returns the extracted list, or null if the operation is invalid.
    /// </summary>
    private List<CardView> ExtractStack(CardView card, object sourcePile)
    {
        if (sourcePile is TableauPile sourceTab)
        {
            return sourceTab.RemoveCards(card);
        }

        if (sourcePile is StockPile sp)
        {
            if (card != sp.GetTopWasteCard()) return null;
            sp.RemoveFromWaste(card);
            return new List<CardView> { card };
        }

        if (sourcePile is FoundationPile fp)
        {
            // Moving from foundation back to tableau is not allowed in this implementation
            _ = fp;
            return null;
        }

        return null;
    }

    /// <summary>
    /// Returns the pile (TableauPile, FoundationPile, or StockPile) that contains
    /// <paramref name="card"/>, or null if not found.
    /// </summary>
    private object FindPileForCard(CardView card)
    {
        foreach (TableauPile tp in tableauPiles)
        {
            if (tp.Cards.Contains(card)) return tp;
        }

        foreach (FoundationPile fp in foundationPiles)
        {
            if (fp.Cards.Contains(card)) return fp;
        }

        if (stockPile.WasteCards.Contains(card)) return stockPile;

        return null;
    }

    private void CheckWin()
    {
        foreach (FoundationPile fp in foundationPiles)
        {
            if (!fp.IsComplete) return;
        }
        ProcessWin();
    }

    private void ProcessWin()
    {
        // Activate the win screen if present in the scene
        if (winScreen == null) return;
        DifficultyType difficulty = GameInstance.instance.Difficulty;
        float gemsAwarded = 0;

        if (difficulty == DifficultyType.Easy) gemsAwarded += 1;
        else gemsAwarded += 5;
        
        winScreen.SetActive(true);
        AudioManager.instance.StopMusic();
        AudioManager.instance.PlaySfx("win");
        AudioManager.instance.PlaySfx("win_music");
        
        if (PurchaseManager.instance.IsGemBoostActive)
        {
            gemBoostUI.SetActive(true);
            gemsAwarded *= 1.5f;
        }

        // Persist the earned gems to the player's account (same auth + Cloud Save
        // pipeline that backs the IAP entitlements).
        PurchaseManager.instance.AddGems(Mathf.RoundToInt(gemsAwarded));
    }

    public void ReturnToMenu()
    {
        if (GameInstance.instance != null)
        {
            GameInstance.instance.GameSeed = 0;
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
    }
}
