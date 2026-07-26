using DG.Tweening;
using UnityEngine;

public class CardAnimator : MonoBehaviour
{
    // Single source of truth for the face-up orientation (the "specific vector").
    private static Quaternion FaceUpWorld => CardView.FaceUpRotation;

    [SerializeField, Range(0f, 1f), Tooltip("Multiplier controlling how much the card arcs upward during move animations.")]
    private float arcAmount = 0.18f;

    private Vector3 defaultScale;
    private Camera  _cam;

    private void Awake()
    {
        defaultScale = transform.localScale;
        _cam = Camera.main;
    }

    /// <summary>
    /// A tiny tilt that spins the card WITHIN the screen plane (about the camera's
    /// view axis). Because it rotates around the direction we're looking from, it
    /// can never lift an edge toward the back face — so the face-down side never
    /// flashes. Falls back to no tilt if there's no camera.
    /// </summary>
    private Quaternion GetScreenTilt(float maxDegrees = 3f)
    {
        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return FaceUpWorld;

        Vector3 viewAxis = _cam.transform.forward;
        return Quaternion.AngleAxis(Random.Range(-maxDegrees, maxDegrees), viewAxis) * FaceUpWorld;
    }

    /// <summary>
    /// "Pop" — quick scale-up, tiny in-plane tilt, and a small lift toward the
    /// camera, settling back with a soft bounce. Played when a card is selected.
    /// </summary>
    public void PlaySelectAnimation()
    {
        transform.DOKill();

        // Clean, known starting state (card is always face-up when selectable).
        transform.rotation   = FaceUpWorld;
        transform.localScale = defaultScale;

        float baseY = transform.position.y;
        Quaternion tilt = GetScreenTilt();

        Sequence seq = DOTween.Sequence();

        // Pop up (~60ms)
        seq.Append(transform.DOScale(defaultScale * 1.05f, 0.06f).SetEase(Ease.OutQuad));
        //seq.Join(transform.DOMoveY(baseY + 0.08f, 0.06f).SetEase(Ease.OutQuad));
        //seq.Join(transform.DORotateQuaternion(tilt, 0.06f).SetEase(Ease.OutQuad));

        // Soft settle back to rest
        seq.Append(transform.DOScale(defaultScale, 0.16f).SetEase(Ease.OutBack));
        //seq.Join(transform.DOMoveY(baseY, 0.16f).SetEase(Ease.OutBack));
        //seq.Join(transform.DORotateQuaternion(FaceUpWorld, 0.16f).SetEase(Ease.OutBack));
    }

    /// <summary>
    /// "Fly" — pops, then arcs to <paramref name="targetWorldPos"/> like it's being
    /// pulled by a magnet, landing with a soft bounce. Used for card moves.
    /// </summary>
    public Tween PlayMoveAnimation(Vector3 targetWorldPos, float duration = 0.32f)
    {
        transform.DOKill();

        transform.rotation   = FaceUpWorld;
        transform.localScale = defaultScale;

        Vector3 start    = transform.position;
        float   distance = Vector3.Distance(start, targetWorldPos);

        // Arc rises out of the table (world +Y = toward camera); scaled to travel.
        float   arcHeight = Mathf.Clamp(distance * arcAmount, 0.12f, 1f);
        Vector3 mid       = Vector3.Lerp(start, targetWorldPos, 0.5f) + Vector3.up * arcHeight;
        Vector3[] path    = { mid, targetWorldPos };

        Quaternion tilt = GetScreenTilt();

        Sequence seq = DOTween.Sequence();

        // Pop before launch — lift above table so card never clips through other piles during flight
        seq.Append(transform.DOScale(defaultScale * 1.05f, 0.06f).SetEase(Ease.OutQuad));
        seq.Join(transform.DORotateQuaternion(tilt, 0.06f).SetEase(Ease.OutQuad));
        seq.Join(transform.DOMoveY(start.y + arcHeight, 0.06f).SetEase(Ease.OutQuad));

        // Magnetic arc — ease out of the source, glide into the destination
        seq.Append(transform.DOPath(path, duration, PathType.CatmullRom).SetEase(Ease.InOutCubic));
        seq.Join(transform.DORotateQuaternion(FaceUpWorld, duration).SetEase(Ease.InOutCubic));

        // Soft bounce on landing
        seq.Append(transform.DOScale(defaultScale * 1.04f, 0.07f).SetEase(Ease.OutQuad));
        seq.Append(transform.DOScale(defaultScale, 0.12f).SetEase(Ease.OutBack));

        // Guarantee an exact final transform regardless of tween rounding.
        seq.OnComplete(() =>
        {
            transform.position   = targetWorldPos;
            transform.rotation   = FaceUpWorld;
            transform.localScale = defaultScale;
        });

        return seq;
    }
}