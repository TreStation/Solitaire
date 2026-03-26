using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BackendManager : MonoBehaviour
{
    public string m_email;

    public void SetEmail(string email)
    {
        m_email = email;
    }

    public void RaffleButtonClicked()
    {
        Debug.Log($"User Email: {m_email}");

        // #TODO Raffle Logic
    }

    public void OnRaffleSuccess()
    {
        GameInstance.instance.PlayerController.State.Gems -= 5;
        GameInstance.instance.SaveToDevice();
        GameInstance.instance.LoadFromDevice();
    }

}
