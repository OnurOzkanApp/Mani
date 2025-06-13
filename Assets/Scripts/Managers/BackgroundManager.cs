using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Sprite Renderer Component of the Background")]
    [SerializeField] private SpriteRenderer backgroundRenderer;

    // Fixed background size for now
    private readonly Vector2 backgroundSize = new Vector2(5.7f, 10f);

    /// <summary>
    /// Handles loading and applying the correct background sprite
    /// based on the current level range (e.g., Levels 1–9, 10–19, etc.).
    /// </summary>
    private void Start()
    {
        // Get the current level number from LevelManager
        int levelNumber = LevelManager.Instance.GetLevelNumber();

        // Adjust range boundaries based on 10, 20, 30... triggers
        int rangeStart = (levelNumber <= 9) ? 1 : (levelNumber / 10) * 10;
        int rangeEnd = (levelNumber <= 9) ? 9 : rangeStart + 9;

        // Set the background's name string based on the level number
        string backgroundName = $"Backgrounds/Background_{rangeStart}_{rangeEnd}";

        // Load the background sprite from Resources folder
        Sprite backgroundSprite = Resources.Load<Sprite>(backgroundName);

        // If the sprite is not null, set it to the background renderer
        if (backgroundSprite != null)
        {
            backgroundRenderer.sprite = backgroundSprite;
        }
        // If the sprite is null, log a warning message
        else
        {
            Debug.LogWarning($"Background sprite not found at path: {backgroundName}");
        }

        // Set the background renderer's draw mode to sliced and size to the defined background size
        backgroundRenderer.drawMode = SpriteDrawMode.Sliced;
        backgroundRenderer.size = backgroundSize;
    }

}
