using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// Handles all input detection (raycasting, single-click, double-click) and fires
/// <see cref="UnityEvent{T}"/> events for consumers. <see cref="PlayerController"/>
/// owns player state and no longer handles input directly.
/// </summary>
public class InputHandler : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Events
    // -------------------------------------------------------------------------

    /// <summary>Fired when a single click lands on a valid card or interactable layer.</summary>
    public UnityEvent<Transform> OnSingleClick = new();

    /// <summary>Fired when a double click lands on a valid card or interactable layer.</summary>
    public UnityEvent<Transform> OnDoubleClick = new();

    /// <summary>Fired when the user clicks with no interactable hit (clears selection).</summary>
    public UnityEvent OnMissClick = new();

    // -------------------------------------------------------------------------
    // Configuration
    // -------------------------------------------------------------------------

    private const float DoubleClickThreshold = 0.4f;

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private int _interactableLayerMask;
    private float _lastClickTime;
    private int _clickCount;
    private Transform _lastHit;
    private RaycastHit _raycastHit;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    private void Start()
    {
        // Layer 6 is the shared interactable layer used by Stock, Cards, and Foundation objects.
        // If named layers ("Card", "Foundation", "Tableau") are later added in Project Settings
        // and objects are moved to those layers, this mask will automatically include them.
        int namedMask = LayerMask.GetMask("Card", "Foundation", "Tableau");
        _interactableLayerMask = (1 << 6) | namedMask;
    }

    private void Update()
    {
        HandleInput();
    }

    // -------------------------------------------------------------------------
    // Input detection
    // -------------------------------------------------------------------------

    private void HandleInput()
    {
        if (!IsClickOrTouch()) return;

        float currentTime = Time.time;
        Ray ray = GetRayFromInput();
        Transform hit = null;

        if (!EventSystem.current.IsPointerOverGameObject()
            && Physics.Raycast(ray, out _raycastHit, Mathf.Infinity, _interactableLayerMask))
        {
            hit = _raycastHit.transform;
        }

        if (hit != null)
        {
            Debug.Log($"[InputHandler] Raycast hit: {hit.name} (layer={hit.gameObject.layer})");
            _lastHit = hit;
            _clickCount++;

            if (_clickCount == 1)
            {
                _lastClickTime = currentTime;
                Invoke(nameof(SingleClickTimeout), DoubleClickThreshold);
            }
            else if (_clickCount == 2 && (currentTime - _lastClickTime) < DoubleClickThreshold)
            {
                CancelInvoke(nameof(SingleClickTimeout));
                OnDoubleClick.Invoke(_lastHit);
                _clickCount = 0;
                _lastHit = null;
            }
            else
            {
                // Second click arrived too late or count drifted — restart as a fresh single-click.
                CancelInvoke(nameof(SingleClickTimeout));
                _clickCount = 1;
                _lastClickTime = currentTime;
                Invoke(nameof(SingleClickTimeout), DoubleClickThreshold);
            }
        }
        else
        {
            Debug.Log($"[InputHandler] Raycast miss — no interactable hit");
            CancelInvoke(nameof(SingleClickTimeout));
            _clickCount = 0;
            _lastHit = null;
            OnMissClick.Invoke();
        }
    }

    private void SingleClickTimeout()
    {
        if (_clickCount == 1 && _lastHit != null)
        {
            OnSingleClick.Invoke(_lastHit);
        }
        _clickCount = 0;
        _lastHit = null;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static bool IsClickOrTouch()
    {
        return Input.GetMouseButtonDown(0)
            || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
    }

    private static Ray GetRayFromInput()
    {
        return Input.touchCount > 0
            ? Camera.main.ScreenPointToRay(Input.GetTouch(0).position)
            : Camera.main.ScreenPointToRay(Input.mousePosition);
    }
}
