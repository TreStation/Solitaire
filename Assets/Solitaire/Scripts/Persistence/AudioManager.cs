using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public struct Sound
{
    public string name;
    public AudioClip clip;
}

/// <summary>
/// Singleton audio manager. Handles a shuffled music playlist and one-shot sound effects.
/// Clips are indexed in a Dictionary on Awake for O(1) lookup.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Sound Effects")]
    public List<Sound> sounds;

    [Header("Music")]
    [Tooltip("Music tracks to be shuffled and played.")]
    public List<AudioClip> musicTracks;

    private AudioSource _musicSource;
    private AudioSource _sfxSource;
    private Dictionary<string, AudioClip> _clipLookup;
    private AudioClip _lastPlayedMusic;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        DontDestroyOnLoad(gameObject);

        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.loop = false; // Loop is handled manually to allow shuffling

        _sfxSource = gameObject.AddComponent<AudioSource>();

        BuildClipLookup();
    }

    private void Start()
    {
        PlayNextShuffledMusic();
    }

    private void Update()
    {
        // If the music source is assigned and has finished playing its clip...
        if (_musicSource != null && !_musicSource.isPlaying && musicTracks.Count > 0)
        {
            // ...play the next song.
            PlayNextShuffledMusic();
        }
    }

    /// <summary>
    /// Stops the background music immediately.
    /// </summary>
    public void StopMusic()
    {
        _musicSource.enabled = false;
    }

    /// <summary>
    /// Plays a sound effect as a one-shot. Does not interrupt the background music.
    /// </summary>
    /// <param name="sfxName">The name of the sound effect clip to play.</param>
    public void PlaySfx(string sfxName)
    {
        if (_clipLookup.TryGetValue(sfxName, out AudioClip clip))
        {
            _sfxSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning($"[AudioManager] SFX clip '{sfxName}' not found.");
        }
    }

    /// <summary>
    /// Selects and plays the next random music track from the playlist.
    /// Avoids playing the same song twice in a row if possible.
    /// </summary>
    private void PlayNextShuffledMusic()
    {
        if (musicTracks == null || musicTracks.Count == 0)
        {
            return; // No music to play.
        }

        List<AudioClip> playableTracks = musicTracks.Where(t => t != null).ToList();
        if (playableTracks.Count > 1 && _lastPlayedMusic != null)
        {
            playableTracks.Remove(_lastPlayedMusic);
        }
        
        if (playableTracks.Count == 0)
        {
            if (musicTracks.Any(t => t != null))
            {
                 playableTracks = musicTracks.Where(t => t != null).ToList();
            }
            else
            {
                return;
            }
        }

        AudioClip clipToPlay = playableTracks[Random.Range(0, playableTracks.Count)];

        _lastPlayedMusic = clipToPlay;
        _musicSource.clip = clipToPlay;
        _musicSource.Play();

        Debug.Log($"[AudioManager] Now Playing: {clipToPlay.name}");
    }

    /// <summary>Builds the dictionary for fast SFX clip lookups.</summary>
    private void BuildClipLookup()
    {
        _clipLookup = new Dictionary<string, AudioClip>(sounds.Count, StringComparer.Ordinal);
        foreach (Sound sound in sounds)
        {
            if (string.IsNullOrEmpty(sound.name) || sound.clip == null) continue;

            if (!_clipLookup.ContainsKey(sound.name))
            {
                _clipLookup.Add(sound.name, sound.clip);
            }
        }
    }
}
