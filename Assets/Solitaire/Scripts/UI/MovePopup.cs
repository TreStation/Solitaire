using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows a short-lived "Nice!" / "Perfect!" badge pinned to the bottom-left
/// corner of the Canvas, animated with a DOTween scale-pop + fade.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class MovePopup : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Header("References")]
    [SerializeField] private Canvas         canvas;
    [SerializeField] private RectTransform  popupRect;
    [SerializeField] private Image          badgeImage;
    [SerializeField] private CanvasGroup    canvasGroup;
    [SerializeField] private Camera         worldCamera;

    [Header("Sprites")]
    [SerializeField] private Sprite niceSprite;
    [SerializeField] private Sprite perfectSprite;

    [Header("Animation")]
    [SerializeField] private float   startScale     = 0.4f;
    [SerializeField] private float   popScale       = 1f;
    [SerializeField] private float   popInDuration  = 0.25f;
    [SerializeField] private float   holdDuration   = 0.5f;
    [SerializeField] private float   fadeOutDuration = 0.3f;
    [Header("Position")]
    [Tooltip("Margin (in canvas units) from the bottom-left corner of the canvas.")]
    [SerializeField] private Vector2 bottomLeftMargin = new Vector2(40f, 40f);

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private RectTransform _canvasRect;
    private Sequence      _sequence;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (worldCamera == null) worldCamera = Camera.main;
        if (canvas != null)      _canvasRect = canvas.transform as RectTransform;

        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Pop the "Nice!" badge near <paramref name="worldPosition"/>.</summary>
    public void ShowNice(Vector3 worldPosition) => Show(false, worldPosition);

    /// <summary>Pop the "Perfect!" badge near <paramref name="worldPosition"/>.</summary>
    public void ShowPerfect(Vector3 worldPosition) => Show(true, worldPosition);

    /// <summary>
    /// Shows the appropriate badge near a world-space position.
    /// </summary>
    /// <param name="perfect">True for "Perfect!" (foundation), false for "Nice!" (tableau).</param>
    /// <param name="worldPosition">World position of the moved card.</param>
    public void Show(bool perfect, Vector3 worldPosition)
    {
        if (badgeImage == null || popupRect == null || _canvasRect == null)
        {
            Debug.LogWarning("[MovePopup] Missing references; cannot show badge.");
            return;
        }

        badgeImage.sprite = perfect ? perfectSprite : niceSprite;

        // Pin the badge to the bottom-left corner of the canvas.
        popupRect.anchorMin        = Vector2.zero;
        popupRect.anchorMax        = Vector2.zero;
        popupRect.pivot            = Vector2.zero;
        popupRect.anchoredPosition = bottomLeftMargin;

        // Scale-pop + fade.
        _sequence?.Kill();
        canvasGroup.alpha    = 0f;
        popupRect.localScale = Vector3.one * startScale;

        _sequence = DOTween.Sequence();
        _sequence.Append(popupRect.DOScale(popScale, popInDuration).SetEase(Ease.OutBack));
        _sequence.Join(canvasGroup.DOFade(1f, popInDuration * 0.6f));
        _sequence.AppendInterval(holdDuration);
        _sequence.Append(canvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad));
    }

    private void OnDestroy() => _sequence?.Kill();
}
