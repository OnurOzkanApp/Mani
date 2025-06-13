using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// UI handler that displays the current level number on the screen.
/// Reads the level number from LevelManager at the start of the scene.
/// </summary>
public class LevelNumberText : MonoBehaviour
{
    [Header("UI Reference")]
    [Tooltip("TextMeshPro component used to display the level number.")]
    [SerializeField] private TMP_Text levelText;

    /// <summary>
    /// Initializes the level number display at the start of the scene.
    /// Retrieves the current level number from the LevelManager and updates the text.
    /// </summary>
    private void Start()
    {
        int currentLevel = LevelManager.Instance.GetLevelNumber();
        levelText.text = currentLevel.ToString();
    }
}
