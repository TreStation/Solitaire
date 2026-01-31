using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameView : MonoBehaviour
{
    public TMP_Text gemsText;
    public Button exitButton;

    public GameObject waste;
    public GameObject stock;
    public GameObject stockModel;
    public GameObject winScreen;
    public bool Animating { get; set; }

    private void Start()
    {
        PlayerController playerController = GameInstance.instance.PlayerController;
        gemsText.text = playerController.State.Gems.ToString();

        exitButton.onClick.AddListener(OnExitButtonPressed);
    }
    private void OnExitButtonPressed()
    {
        SceneManager.LoadScene("Menu");
    }

    // Animations
    public void FlipCard(Card card)
    {
        AnimateCardFlip(card, 0.35f, 0.2f, 0.75f, new Vector3(0, 90, 0));
    }

    public void AnimateCardToTableau(Card card, Card targetCard)
    {
        Vector3 target = targetCard.transform.position + new Vector3(0, 0.005f, -0.2f);
        AnimateCardPath(card, GetPath(card.transform.position, target, 0.85f), 0.75f, Ease.InOutQuad, () => Animating = false);
    }

    public void AnimateKing(Card card, Pile tableau)
    {
        AnimateCardPath(card, GetPath(card.transform.position, tableau.transform.position, 1.67f), 0.65f, Ease.InOutQuad, () => Animating = false);
    }

    public void AnimateCardToFoundation(Card card, Pile foundation)
    {
        Vector3 target = foundation.transform.position + new Vector3(0, 0.01f, 0);
        AnimateCardPath(card, GetPath(card.transform.position, target, 0.75f), 0.75f, Ease.InOutQuad, () =>
        {
            Animating = false;
            if (foundation.displayCard != null) Destroy(foundation.displayCard);
            foundation.displayCard = card.gameObject;
        });
    }

    public void AnimateToWaste(Card card, int wasteSlot)
    {
        float liftDuration = 0.15f;
        float moveDuration = 0.5f;
        float altitude = 0.5f;
        float[] offsets = { 1f, 0.9f, 0.8f };
        float offset = offsets[wasteSlot - 1];

        Vector3 target = waste.transform.position;
        target.x *= offset;
        target.y += 0.01f * wasteSlot;

        card.transform.DOLocalMoveY(altitude, liftDuration).OnComplete(() =>
        {

            card.transform.DOMove(target, moveDuration).SetEase(Ease.InOutQuad).OnComplete(() => {
                Animating = false;
            });
        });
    }

    // Helper Methods
    private void AnimateCardFlip(Card card, float duration, float delay, float moveY, Vector3 rotate)
    {
        float initialY = card.transform.position.y;
        DOVirtual.DelayedCall(delay, () =>
        {
            card.transform.DOLocalMoveY(moveY, duration).OnComplete(() =>
            {
                card.transform.DORotate(rotate, duration).OnComplete(() =>
                {
                    card.transform.DOLocalMoveY(initialY, duration);
                });
            });
            card.Active = true;
        });
    }

    public void AnimateCardMove(Card card, Vector3 target, float duration, Ease ease, TweenCallback onComplete)
    {
        Animating = true;
        card.transform.DOMove(target, duration).SetEase(ease).OnComplete(onComplete);
    }

    private void AnimateCardPath(Card card, Vector3[] path, float duration, Ease ease, TweenCallback onComplete = null)
    {
        Animating = true;
        card.transform.DOPath(path, duration, PathType.CatmullRom).SetEase(ease).OnComplete(onComplete);
    }

    private Vector3[] GetPath(Vector3 origin, Vector3 target, float altitude)
    {
        origin.y += altitude;
        return new[] { origin, target };
    }

    public void ToggleStock(bool active) 
    {
        if (active) 
        {
            stockModel.SetActive(true);
        }
        else 
        {
            stockModel.SetActive(false);
        }
    }

    public void DisplayWin()
    {
        winScreen.SetActive(true);
    }
}
