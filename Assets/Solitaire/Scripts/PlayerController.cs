using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour
{
    public PlayerData State { get; set; }

    private Transform currentSelection;
    private Transform cachedSelection;
    private RaycastHit raycastHit;

    private readonly Color outlineColor = Color.magenta;
    private readonly float outlineWidth = 5f;
    private readonly float doubleClickThreshold = 0.25f;

    private float lastClickTime = 0f;
    private int clickCount = 0;
    private int interactableLayerMask;

    private void Start()
    {
        interactableLayerMask = LayerMask.GetMask("Card", "Foundation", "Tableau");
    }

    private void Update()
    {
        HandleSelection();
    }

    private void HandleSelection()
    {
        if (IsClickOrTouch())
        {
            float currentTime = Time.time;
            Ray ray = GetRayFromInput();

            if (!EventSystem.current.IsPointerOverGameObject() && Physics.Raycast(ray, out raycastHit, Mathf.Infinity, interactableLayerMask))
            {
                currentSelection = raycastHit.transform;
            }

            if (currentSelection)
            {
                clickCount++;
                if (clickCount == 1)
                {
                    lastClickTime = currentTime;
                    Invoke(nameof(SingleClickTimeout), doubleClickThreshold);
                }
                else if (clickCount == 2 && (currentTime - lastClickTime) < doubleClickThreshold)
                {
                    CancelInvoke(nameof(SingleClickTimeout));
                    OnDoubleClick(currentSelection);
                    clickCount = 0;
                }
            }
            else
            {
                ClearSelections();
            }
        }
    }

    private bool IsClickOrTouch()
    {
        return Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
    }

    private Ray GetRayFromInput()
    {
        return Input.touchCount > 0
            ? Camera.main.ScreenPointToRay(Input.GetTouch(0).position)
            : Camera.main.ScreenPointToRay(Input.mousePosition);
    }

    private void SingleClickTimeout()
    {
        if (clickCount == 1)
        {
            OnSingleClick();
        }
        clickCount = 0;
    }

    private void OnSingleClick()
    {
        if (currentSelection.name == "Stock")
        {
            GameManager.instance.OnStockClicked();
            ClearSelections();
            return;
        }

        if (currentSelection.TryGetComponent<Card>(out var targetCard))
        {
            if (!targetCard.Active || !IsSelectable(currentSelection))
            {
                ClearSelections();
                return;
            }

            EnableOutline(currentSelection);

            if (cachedSelection)
            {
                HandleCachedSelection(targetCard);
            }
            else
            {
                cachedSelection = currentSelection;
            }
        }
        else if (currentSelection.gameObject.layer == LayerMask.NameToLayer("Tableau"))
        {
            if (cachedSelection && cachedSelection.TryGetComponent<Card>(out var selectedCard) && selectedCard.rank == Rank.King)
            {
                GameManager.instance.KingMove(selectedCard, currentSelection);
                ClearSelections();
            }
        }
        else if (currentSelection.gameObject.layer == LayerMask.NameToLayer("Foundation"))
        {
            if (cachedSelection && cachedSelection.TryGetComponent<Card>(out var selectedCard))
            {
                GameManager.instance.OnCardDoubleClicked(selectedCard);
                ClearSelections();
            }
        }
        else
        {
            ClearSelections();
        }
    }

    private bool IsSelectable(Transform selection)
    {
        return selection.CompareTag("Selectable") && selection != cachedSelection;
    }

    private void HandleCachedSelection(Card targetCard)
    {
        Card selectedCard = cachedSelection.GetComponent<Card>();

        if (selectedCard && targetCard)
        {
            GameManager.instance.HandleTableauMove(selectedCard, targetCard.parent.pile);
        }
        ClearSelections();
    }

    private void OnDoubleClick(Transform clickedTransform)
    {
        if (clickedTransform.TryGetComponent<Card>(out var clickedCard))
        {
            GameManager.instance.OnCardDoubleClicked(clickedCard);
        }
        ClearSelections();
    }

    private void EnableOutline(Transform target)
    {
        if (target.TryGetComponent<Outline>(out var outline))
        {
            outline.OutlineColor = outlineColor;
            outline.OutlineWidth = outlineWidth;
            outline.enabled = true;
        }
    }

    private void DisableOutline(Transform target)
    {
        if (target.TryGetComponent<Outline>(out var outline))
        {
            outline.enabled = false;
        }
    }

    private void ClearSelections()
    {
        if (currentSelection != null)
        {
            DisableOutline(currentSelection);
            currentSelection = null;
        }
        if (cachedSelection != null)
        {
            DisableOutline(cachedSelection);
            cachedSelection = null;
        }
    }
}
