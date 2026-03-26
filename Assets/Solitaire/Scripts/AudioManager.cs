using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct Sound
{
    public string name;
    public AudioClip clip;
}

public class AudioManager : MonoBehaviour
{
    public List<Sound> sounds;
    private AudioSource player;

    void Awake()
    {
        if (GameObject.FindObjectsByType<AudioManager>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        player = gameObject.AddComponent<AudioSource>();
        PlayAudioClip("Theme");
    }

    public void PlayAudioClip(string clipName)
    {
        foreach (var sound in sounds)
        {
            if (sound.name == clipName)
            {
                player.clip = sound.clip;
                player.Play();
            }
        }
    }
}
