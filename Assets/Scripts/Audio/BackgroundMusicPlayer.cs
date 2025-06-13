using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundMusicPlayer : MonoBehaviour
{
    // Singleton instance for BackgroundMusicPlayer
    public static BackgroundMusicPlayer Instance { get; private set; }

    // Audio clips for main menu and level music
    public AudioClip mainMenuMusic;
    public AudioClip levelMusic;

    // Variable to store the playback time of the level music
    private float levelMusicTime;

    [Header("References")]
    [Tooltip("Audio Source to play music from.")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Low Pass Filter to lower the audio.")]
    [SerializeField] private AudioLowPassFilter lowPassFilter;

    /// <summary>
    /// Checks if the instance already exists, if not, sets this as the instance.
    /// If an instance already exists, destroys this GameObject to avoid duplicates.
    /// Does not destroy this GameObject on scene load, allowing it to persist across scenes.
    /// Sets the initial music volume based on PlayerPrefs, defaulting to 0.3f if not set.
    /// </summary>
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Do not destroy this GameObject on scene load to keep the music playing across scenes
        DontDestroyOnLoad(gameObject);
        // Set the initial music volume from PlayerPrefs, defaulting to 0.3f if not set
        SetMusicVolume(PlayerPrefs.GetFloat("musicVolume", 0.3f));
    }

    /// <summary>
    /// Plays the main menu music from the beginning.
    /// </summary>
    public void PlayMainMenuMusic()
    {
        SaveCurrentPlaybackTime();
        audioSource.clip = mainMenuMusic;
        audioSource.time = 0f;
        audioSource.Play();
    }

    /// <summary>
    /// Plays the level music from the beginning or resumes from the last saved position,
    /// depending on the resume flag.
    /// </summary>
    /// <param name="resume">The flag indicating whether to resume background music; defaults to false.</param>
    public void PlayLevelMusic(bool resume = false)
    {
        // Check if the audio clip is already Level music and if it is playing, if so, return early
        if (audioSource.clip == levelMusic && audioSource.isPlaying)
        {
            return;
        }
        // Set the audio to play as level music and check if we should resume playback
        audioSource.clip = levelMusic;
        if (resume)
        {
            audioSource.time = levelMusicTime;
        }
        else
        {
            audioSource.time = 0f;
        }
        audioSource.Play();
    }

    /// <summary>
    /// Saves the current playback time of the level music to allow resuming later.
    /// </summary>
    public void SaveCurrentPlaybackTime()
    {
        if (audioSource.clip == levelMusic)
            levelMusicTime = audioSource.time;
    }

    /// <summary>
    /// Filters the audio using a low pass filter with the specified cutoff frequency.
    /// </summary>
    /// <param name="cutoff">The cutoff frequency of the low pass filter.</param>
    public void ApplyLowPass(float cutoff)
    {
        if (lowPassFilter == null)
        {
            return;
        }

        lowPassFilter.cutoffFrequency = cutoff;
    }

    /// <summary>
    /// Changes the music volume and saves it to PlayerPrefs.
    /// </summary>
    /// <param name="volume">The desired music volume, saved to PlayerPrefs for persistence.</param>
    public void SetMusicVolume(float volume)
    {
        audioSource.volume = volume;
        PlayerPrefs.SetFloat("musicVolume", volume);
    }

    /// <summary>
    /// Returns the current music volume.
    /// </summary>
    public float GetCurrentVolume()
    {
        return audioSource.volume;
    }

    /// <summary>
    /// Returns whether sound effects are enabled or not based on PlayerPrefs.
    /// </summary>
    public static bool SoundFXEnabled
    {
        get => PlayerPrefs.GetInt("soundFXEnabled", 1) == 1;
        set
        {
            PlayerPrefs.SetInt("soundFXEnabled", value ? 1 : 0);
        }
    }

    /// <summary>
    /// Turns sound effects on or off by toggling the SoundFXEnabled property.
    /// </summary>
    public static void ToggleSoundFX()
    {
        SoundFXEnabled = !SoundFXEnabled;
    }
}
