using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{

    // Singleton instance reference for Score Manager
    public static ScoreManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("TextMeshPro Text for the score")]
    [SerializeField] private TMP_Text scoreText;

    // Current score value
    private int currentScore;

    /// <summary>
    /// Checks if the instance already exists, if not, sets this as the instance.
    /// If an instance already exists, destroys this GameObject to avoid duplicates.
    /// Resets the score to zero at the start.
    /// </summary>
    private void Awake()
    {
        // Ensure only one instance of ScoreManager exists
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Reset the score to zero at the start
        ResetScore();
    }

    /// <summary>
    /// Resets the score to zero and updates the UI.
    /// </summary>
    public void ResetScore()
    {
        currentScore = 0;
        UpdateUI();
    }

    /// <summary>
    /// Adds the given amount to the current score and updates the UI. 
    /// </summary>
    /// <param name="amount">The amount to add to the current score.</param>
    public void AddScore(int amount)
    {
        currentScore += amount;
        UpdateUI();
    }

    /// <summary>
    /// Returns the current score.
    /// </summary>
    public int GetScore()
    {
        return currentScore;
    }

    /// <summary>
    /// Updates the UI text to reflect the current score.
    /// </summary>
    private void UpdateUI()
    {
        // Check if scoreText is assigned before updating to avoid null reference errors
        if (scoreText != null)
            // Update the score text with the current score
            scoreText.text = currentScore.ToString();
    }

    /// <summary>
    /// Calculates the score to add depending on the cube group that was destroyed. 
    /// </summary>
    /// <param name="group">A list of cubes inside a cube group.</param>
    public int CalculateMatchScore(List<Cube> group)
    {
        // Initialize score to zero
        int score = 0;

        // If the group is null or empty, return the score as zero
        if (group == null || group.Count == 0) return score;

        // Calculate score based on the number of cubes in the group at the start
        score += group.Count * 10;

        // Add additional score based on specific conditions
        // If the group has more than 5 or more cubes, add 50 points
        if (group.Count >= 5) score += 50;
        // Add 30 bonus points if the group includes a special cube
        if (group.Any(c => c.IsSpecial)) score += 30;

        // Create a HashSet to store unique adjacent obstacles
        HashSet<Obstacle> adjacentObstacles = new();
        // Loop through each cube in the group to find adjacent obstacles
        foreach (Cube cube in group)
        {
            // Get the indices of the cube
            int x = cube.GetX();
            int y = cube.GetY();
            // Check each direction (up, down, left, right) for adjacent obstacles
            foreach (Vector2Int dir in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                // Get the neighbor tile at the specified direction
                var neighborTile = GameBoard.Instance.GetBoardTileAt(x + dir.x, y + dir.y);
                // Set the neighbor GameObject to null initially
                var neighbor = (GameObject)null;
                // If the neighbor tile is not null, get the object inside it
                if (neighborTile != null)
                {
                    neighbor = neighborTile.GetObjectInside();

                }
                // If the neighbor is not null and is an Obstacle, add it to the HashSet of adjacent obstacles
                if (neighbor != null && neighbor.TryGetComponent(out Obstacle obs))
                    adjacentObstacles.Add(obs);
            }
        }

        // Add the score based on the number of unique adjacent obstacles found
        score += adjacentObstacles.Count * 100;
        // Return the calculated score
        return score;
    }
}
