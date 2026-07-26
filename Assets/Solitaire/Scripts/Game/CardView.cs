using System;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// MonoBehaviour attached to every card GameObject.
/// Owns all per-card visuals: face texture, face-up/down orientation, and outline visibility.
/// </summary>
[RequireComponent(typeof(Outline))]
public class CardView : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Constants — adjust if face-up/down rotation looks wrong after first run
    // -------------------------------------------------------------------------

    public  static readonly Quaternion FaceUpRotation   = Quaternion.Euler(0f, 0f, 180f);
    private static readonly Quaternion FaceDownRotation = Quaternion.Euler(0f,   0f, 0f);

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    public CardData Data { get; private set; }

    private MeshRenderer _renderer;
    private Outline      _outline;
    private Collider     _collider;
    private CardAnimator _cardAnimator;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        _outline  = GetComponent<Outline>();
        _renderer = GetComponentInChildren<MeshRenderer>();
        _collider = GetComponent<Collider>();
        _cardAnimator = GetComponent<CardAnimator>();

        // Outline starts disabled
        if (_outline != null)
        {
            _outline.enabled = false;
        }
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Assigns card data and loads the face texture from Resources.
    /// </summary>
    public void Initialize(CardData data)
    {
        Data = data;
        CardArtType cardArt = GameInstance.instance.CardArt;
        
        string frontPath = BuildResourcePath(data.Suit, data.Rank);
        string backPath  = BuildResourcePathForCardArt(cardArt);
        
        Texture2D frontTexture = Resources.Load<Texture2D>(frontPath);
        Texture2D backTexture = Resources.Load<Texture2D>(backPath);

        if (frontTexture != null && _renderer != null)
        {
            // renderer.materials creates per-instance material copies (intentional — 52 cards, acceptable overhead)
            // UV is pre-flipped 180° so the face reads correctly after the Z-axis card-flip animation.
            Material[] mats = _renderer.materials;
            mats[1].mainTexture       = frontTexture;
            mats[1].mainTextureScale  = new Vector2(-1f, -1f);
            mats[1].mainTextureOffset = new Vector2(1f, 1f);
            mats[2].mainTexture       = backTexture;
            mats[2].mainTextureScale  = new Vector2(1f, 1f);
            mats[2].mainTextureOffset = new Vector2(1f, 1f);
            _renderer.materials = mats;
        }
        else if (frontTexture == null)
        {
            Debug.LogWarning($"[CardView] Could not load texture at Resources/{frontPath}");
        }
        else if (backTexture == null)
        {
            Debug.LogWarning($"[CardView] Could not load texture at Resources/{backPath}");
        }

        if (backTexture == null)
        {
            Debug.LogWarning($"[CardView] Could not load back texture at Resources/{backPath} (CardArt={GameInstance.instance.CardArt})");
        }

        SetFaceUp(data.IsFaceUp);
    }

    /// <summary>
    /// Snaps the card to face-up or face-down orientation without animation.
    /// Also syncs the logical <see cref="CardData.IsFaceUp"/> flag.
    /// </summary>
    public void SetFaceUp(bool faceUp)
    {
        Data = new CardData(Data.Suit, Data.Rank, faceUp);
        transform.rotation = faceUp ? FaceUpRotation : FaceDownRotation;
    }

    /// <summary>Enables or disables the QuickOutline component.</summary>
    public void SetOutline(bool enabled)
    {
        if (_outline != null)
        {
            _outline.enabled = enabled;
        }
    }

    public void PlayAnimation(string animationName)
    {
        switch (animationName)
        {
            case "Selection":
                _cardAnimator.PlaySelectAnimation();
                AudioManager.instance.PlaySfx("card_click");
                AudioManager.instance.PlaySfx("card_chime");
                break;
        }
    }

    /// <summary>
    /// Animates the card flying along a magnetic arc to <paramref name="worldPos"/>.
    /// Falls back to an instant snap if no <see cref="CardAnimator"/> is present.
    /// </summary>
    public void AnimateMoveTo(Vector3 worldPos)
    {
        if (_cardAnimator != null)
        {
            _cardAnimator.PlayMoveAnimation(worldPos);
        }
        else
        {
            transform.position = worldPos;
        }
    }

    /// <summary>Enables or disables the card's collider (used for waste pile management).</summary>
    public void SetColliderEnabled(bool enabled)
    {
        if (_collider != null)
        {
            _collider.enabled = enabled;
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static string BuildResourcePath(SuitType suit, RankType rank)
    {
        string suffix = RankSuffix(rank);
        return suit switch
        {
            SuitType.Clubs    => $"Clubs/B_Club_{suffix}",
            SuitType.Spades   => $"Spades/B_Spade_{suffix}",
            SuitType.Hearts   => $"Hearts/R_Heart_{suffix}",
            SuitType.Diamonds => $"Diamonds/R_Diamond_{suffix}",
            _                 => string.Empty
        };
    }

    private static string BuildResourcePathForCardArt(CardArtType cardArt)
    {
        return cardArt switch
        {
            CardArtType.Blue   => $"Back/Blue_Back",
            CardArtType.Red   => $"Back/Red_Back",
            CardArtType.Palm   => $"Back/Palm_Back",
            _                 => string.Empty
        };
    }

    private static string RankSuffix(RankType rank) => rank switch
    {
        RankType.Ace   => "A",
        RankType.Two   => "2",
        RankType.Three => "3",
        RankType.Four  => "4",
        RankType.Five  => "5",
        RankType.Six   => "6",
        RankType.Seven => "7",
        RankType.Eight => "8",
        RankType.Nine  => "9",
        RankType.Ten   => "10",
        RankType.Jack  => "J",
        RankType.Queen => "Q",
        RankType.King  => "K",
        _              => string.Empty
    };

}
