using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    /// <summary>
    /// Deactivates the pause menu, effectively resuming the game.
    /// </summary>
    public void ResumeGame()
    {
        this.gameObject.SetActive(false);
    }

    /// <summary>
    /// Activates the pause menu, effectively pausing the game.
    /// </summary>
    public void PauseGame()
    {
        this.gameObject.SetActive(true);
    }

    /// <summary>
    /// Changes the pause state of the game. If the pause menu is active,
    /// it resumes the game; otherwise, it pauses the game.
    /// </summary>
    public void ChangePauseState()
    {
        if (gameObject.activeSelf)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    /// <summary>
    /// Returns whether the game is currently paused based on the active state of the pause menu.
    /// </summary>
    public bool CheckIfPaused()
    {
        if (this.gameObject.activeSelf)
        {
            return true;
        } else
        {
            return false;
        }
    }
}
