using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Manages level scene UI interactions including pause menu, settings, 
/// win/lose screens, and navigation buttons like replay and main menu.
/// </summary>
public class LevelMenuManager : MonoBehaviour
{
    [Header("Popup References")]
    [Tooltip("Reference to the PopUps manager for displaying win/lose messages.")]
    [SerializeField] private PopUps popUps;

    [Header("Pause Menu")]
    [Tooltip("Reference to the Pause Menu script that controls pause state.")]
    [SerializeField] private PauseMenu pauseMenu;
    [Tooltip("Button to close the Pause Menu.")]
    [SerializeField] private GameObject closePauseMenuButton;
    [Tooltip("Button to replay the level.")]
    [SerializeField] private GameObject replayButton;
    [Tooltip("Button to resume the game.")]
    [SerializeField] private GameObject resumeGameButton;
    [Tooltip("Button to open the Settings Menu.")]
    [SerializeField] private GameObject settingsButton;
    [Tooltip("Button to go back to the Main Menu.")]
    [SerializeField] private GameObject backToMainMenuButton;

    [Header("Settings Menu")]
    [Tooltip("Parent GameObject for the settings menu UI.")]
    [SerializeField] private GameObject settingsMenu;

    /// <summary>
    /// Checks if the escape key is pressed to open/close the pause menu.
    /// </summary>
    private void Update()
    {
        // Check if the escape key is pressed to open/close the pause menu
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            pauseMenu.ChangePauseState();
        }
    }

    /// <summary>
    /// Opens or closes the pause menu based on its current state.
    /// </summary>
    public void SwitchPauseMenu()
    {
        // Open the Pause Menu if it is closed, close it if it is open
        pauseMenu.ChangePauseState();

    }

    /// <summary>
    /// Quits the game when the Quit button is clicked.
    /// </summary>
    public void ClickOnQuit()
    {
        // If the player click on the Quit the game Button
        Application.Quit();
    }

    /// <summary>
    /// Opens the settings menu by activating the Settings Menu Game Object
    /// when the Settings button is clicked.
    /// </summary>
    public void OpenUpSettings()
    {
        settingsMenu.SetActive(true);
    }

    /// <summary>
    /// Replays the current level by despawning all objects on the board and
    /// reloading the level scene.
    /// </summary>
    public void ReplayLevel()
    {
        // Despawning all board objects before restarting the level
        if (GameBoard.Instance != null)
            GameBoard.Instance.DespawnAllBoardObjects();
        // Restart the current level by loading the level scene again
        SceneController.Instance.LoadLevelScene();
    }

    /// <summary>
    /// Opens the main menu by loading the main scene.
    /// </summary>
    public void OpenMainMenu()
    {
        // Go back to the main menu
        SceneController.Instance.LoadMainScene();
    }

    /// <summary>
    /// Closes the win screen pop-up when the player clicks on the "Next Level" button.
    /// </summary>
    public void OpenNextLevel()
    {
        popUps.CloseWinScreen();
    }
}
