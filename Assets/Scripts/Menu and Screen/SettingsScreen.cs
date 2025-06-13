using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles music volume adjustment and sound FX toggle for the Settings screen.
/// Updates UI visuals and communicates with the BackgroundMusicPlayer to apply settings.
/// </summary>
public class SettingsScreen : MonoBehaviour
{
    [Header("Music Settings")]
    [Tooltip("Slider used to control background music volume.")]
    [SerializeField] private Slider musicSlider;

    [Header("Sound FX Settings")]
    [Tooltip("UI image that reflects the sound FX on/off state visually.")]
    [SerializeField] private Image soundFXToggleImage;
    [Tooltip("Sprite to show when sound FX is enabled.")]
    [SerializeField] private Sprite soundFXOnSprite;
    [Tooltip("Sprite to show when sound FX is disabled.")]
    [SerializeField] private Sprite soundFXOffSprite;

    /// <summary>
    /// Initializes the music slider and sound FX icon when settings screen is shown.
    /// </summary>
    private void OnEnable()
    {
        // Makes the sound setting available only if the BackgroundMusicPlayer instance exists
        if (BackgroundMusicPlayer.Instance != null)
        {
            // Prevent multiple callbacks stacking on the slider
            musicSlider.onValueChanged.RemoveAllListeners();
            // Add the volume update method
            musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
            // Set slider without triggering callback
            musicSlider.SetValueWithoutNotify(BackgroundMusicPlayer.Instance.GetCurrentVolume());
        }

        // Updates the sound FX icon based on the current state
        UpdateSoundFXVisual();
    }

    /// <summary>
    /// Called when the music slider value changes.
    /// Updates the background music volume.
    /// </summary>
    /// <param name="value">New music volume between 0 and 1.</param>
    public void OnMusicSliderChanged(float value)
    {
        BackgroundMusicPlayer.Instance?.SetMusicVolume(value);
    }

    /// <summary>
    /// Toggles the sound FX enabled state and updates the icon.
    /// </summary>
    public void ToggleSoundFX()
    {
        BackgroundMusicPlayer.SoundFXEnabled = !BackgroundMusicPlayer.SoundFXEnabled;
        UpdateSoundFXVisual();
    }

    /// <summary>
    /// Updates the icon based on current sound FX state.
    /// </summary>
    private void UpdateSoundFXVisual()
    {
        if (BackgroundMusicPlayer.SoundFXEnabled)
        {
            soundFXToggleImage.sprite = soundFXOnSprite;
        }
        else
        {
            soundFXToggleImage.sprite = soundFXOffSprite;
        }
    }

    /// <summary>
    /// Closes the settings screen by deactivating the GameObject.
    /// </summary>
    public void CloseSettings()
    {
        gameObject.SetActive(false);
    }
}
