using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Manages the display and closing logic of the win and lose screens.
/// Also provides utility checks for whether a screen is currently active.
/// </summary>
public class PopUps : MonoBehaviour
{
    [Header("Popup References")]
    [Tooltip("UI element shown when the player wins a level.")]
    [SerializeField] private GameObject winScreen;

    [Tooltip("UI element shown when the player loses a level.")]
    [SerializeField] private GameObject loseScreen;

    /// <summary>
    /// Displays the win screen popup.
    /// </summary>
    public void ShowWinScreen()
    {
        winScreen.gameObject.SetActive(true);
    }

    /// <summary>
    /// Closes the win screen and proceeds to the next level.
    /// </summary>
    public void CloseWinScreen()
    {
        winScreen.gameObject.SetActive(false);
        LevelManager.Instance.GoToTheNextLevel();
    }

    /// <summary>
    /// Returns true if the win screen is currently shown.
    /// </summary>
    public bool CheckIfLevelCleared()
    {
        return winScreen.gameObject.activeSelf;
    }

    /// <summary>
    /// Displays the lose screen popup.
    /// </summary>
    public void ShowLoseScreen()
    {
        loseScreen.gameObject.SetActive(true);
    }

    /// <summary>
    /// Closes the lose screen popup.
    /// </summary>
    public void CloseLoseScreen()
    {
        loseScreen.gameObject.SetActive(false);
    }

    /// <summary>
    /// Returns true if the lose screen is currently shown.
    /// </summary>
    public bool CheckIfLevelLost()
    {
        return loseScreen.gameObject.activeSelf;
    }
}
