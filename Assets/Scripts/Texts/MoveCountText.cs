using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// UI handler that displays the current number of moves remaining in the level.
/// Reads from LevelManager at the start and updates text as needed during gameplay.
/// </summary>
public class MoveCountText : MonoBehaviour
{
    [Header("UI Reference")]
    [Tooltip("TextMeshPro component used to display the move count.")]
    [SerializeField] private TMP_Text moveCountText;

    /// <summary>
    /// Initializes the move count display at the start of the level.
    /// Retrieves the current move count from the LevelManager.
    /// </summary>
    private void Start()
    {
        int moveCount = LevelManager.Instance.GetMoveCount();
        moveCountText.text = moveCount.ToString();
    }

    /// <summary>
    /// Updates the displayed move count.
    /// </summary>
    /// <param name="newCount">The new move count to display, as a string.</param>
    public void ChangeMoveCountText(string newCount)
    {
        moveCountText.text = newCount;
    }
}
