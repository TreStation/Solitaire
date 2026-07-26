using System;
using UnityEngine;

public class WinScreen : MonoBehaviour
{
    public GameObject gemBoostUI;
    private void Awake()
    {
        
        // Win Effects
        PlayWinAudio();
        gemBoostUI.SetActive(false);
        
        Debug.Log("GEM_BOOST ACTIVE: " + PurchaseManager.instance.IsGemBoostActive + "");
        if (PurchaseManager.instance.IsGemBoostActive)
        {
            gemBoostUI.SetActive(true);
        }
    }

    private void PlayWinAudio()
    {
        AudioManager.instance.StopMusic();
        AudioManager.instance.PlaySfx("win");
        AudioManager.instance.PlaySfx("win_music");  
    }
}
