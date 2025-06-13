using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the main menu UI, including navigation to settings, starting the level, 
/// and the load menu. Also triggers background music at startup.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Parent GameObject for the settings menu UI.")]
    [SerializeField] private GameObject settingsScreen;
    [Tooltip("Panel GameObject for the load menu UI.")]
    [SerializeField] private GameObject loadMenuPanel;

    // TODO: Uncomment and implement the close button functionality if needed
    // [Tooltip("Button to close the game from the main menu.")]
    // [SerializeField] private GameObject closeButton;

    /// <summary>
    /// Plays the main menu music when the game starts.
    /// </summary>
    private void Awake()
    {
        // Play the main menu music when the game starts
        BackgroundMusicPlayer.Instance.PlayMainMenuMusic();
    }

    /// <summary>
    /// Starts the level by loading the level scene.
    /// </summary>
    public void PlayLevel()
    {
        // Start the level by loading the level scene
        SceneController.Instance.LoadLevelScene();
    }

    /// <summary>
    /// Opens the settings menu by activating the settings screen GameObject.
    /// </summary>
    public void OpenUpSettings()
    {
        settingsScreen.SetActive(true);
    }

    /// <summary>
    /// Opens the load menu by activating the load menu panel GameObject.
    /// </summary>
    public void OpenLoadMenu()
    {
        loadMenuPanel.SetActive(true);
    }
}
