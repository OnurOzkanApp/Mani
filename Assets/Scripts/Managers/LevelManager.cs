using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

/// <summary>
/// Handles level state, including loading level data, managing UI updates,
/// tracking move and target counts, and handling win/lose conditions.
/// </summary>
public class LevelManager : MonoBehaviour
{
    // Singleton instance
    public static LevelManager Instance { get; private set; }

    // Static variable to keep track of the current level index
    private static int currentLevel = 0;

    // Level-specific data
    private int levelNumber;
    private int gridWidth;
    private int gridHeight;
    private int moveCount;
    private List<string> board;
    private JsonReader.LevelTarget[] targets;

    // Total and destroyed target counts
    private int totalTargetCount;
    private int destroyedTargetCount;

    // Dictionary to keep track of current target counts by type
    private Dictionary<string, int> currentTargetCounts;

    [Header("UI Managers")]
    [Tooltip("Manages the visual display and updating of target objectives.")]
    [SerializeField] private TargetsUIManager targetsUIManager;
    [Tooltip("Controls the progress bar that shows level completion based on destroyed targets.")]
    [SerializeField] private ProgressBarManager progressBarManager;
    [Tooltip("Displays the current remaining move count in the UI.")]
    [SerializeField] private MoveCountText moveCountText;
    [Tooltip("Handles the display of pop-up messages such as win/lose screens.")]
    [SerializeField] private PopUps popUps;

    /// <summary>
    /// Checks if the instance already exists, if not, sets this as the instance.
    /// If an instance already exists, destroys this GameObject to avoid duplicates.
    /// Loads the data for the level. If the previous scene was "LevelScene", it resumes the background music.
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Duplicate LevelManager found.");
            Destroy(gameObject);
            return;
        }

        // Load the level data
        LoadLevelData();

        // Check if the previous scene was "LevelScene" to determine if we should resume music
        bool resume = SceneController.GetPreviousSceneName() == "LevelScene";
        // Resume background music if applicable
        if (BackgroundMusicPlayer.Instance != null)
        {
            BackgroundMusicPlayer.Instance.PlayLevelMusic(resume);
        }

    }

    /// <summary>
    /// Updates the current level to the new index provided.
    /// </summary>
    /// <param name="index">The index of the new level.</param>
    public static void SetLevelIndex(int index)
    {
        currentLevel = index;
    }

    public int GetLevelNumber() => levelNumber;
    public int GetGridWidth() => gridWidth;
    public int GetGridHeight() => gridHeight;
    public int GetMoveCount() => moveCount;
    public List<string> GetBoardLayout() => board;
    public JsonReader.LevelTarget[] GetLevelTargets() => targets;

    /// <summary>
    /// Sets the level-specific data for the current level by loading it from
    /// allLevels list inside JSONReader. Then sets the targets for the current level.
    /// </summary>
    private void LoadLevelData()
    {
        progressBarManager.SetProgressAtTheStart(0f);

        var currentLevelData = JsonReader.Instance.GetAllLevels()[currentLevel];

        levelNumber = currentLevelData.level_number;
        gridWidth = currentLevelData.grid_width;
        gridHeight = currentLevelData.grid_height;
        moveCount = currentLevelData.move_count;
        board = currentLevelData.board;
        targets = currentLevelData.targets;

        currentTargetCounts = new Dictionary<string, int>();
        if (targets != null)
        {
            foreach (var target in targets)
                currentTargetCounts[target.targetType] = target.count;
        }

        destroyedTargetCount = 0;
        totalTargetCount = currentTargetCounts.Values.Sum();
    }

    /// <summary>
    /// Decreases the move count by one and updates the UI text accordingly.
    /// </summary>
    public void DecreaseMoveCount()
    {
        moveCount--;
        moveCountText.ChangeMoveCountText(moveCount.ToString());
    }

    /// <summary>
    /// Returns true if all targets for the level are destroyed, false otherwise.
    /// </summary>
    public bool CheckIfAllTargetsAreDestroyed()
    {
        return GetRemainingNumberOfTargets() == 0;
    }

    /// <summary>
    /// Returns the total number of targets that are still remaining in the level.
    /// </summary>
    private int GetRemainingNumberOfTargets()
    {
        return currentTargetCounts.Values.Sum();
    }

    /// <summary>
    /// Starts the win screen by stopping the post-clear glow effect on the progress bar,
    /// and save the level progress.
    /// </summary>
    public void OpenWinScreen()
    {
        progressBarManager.StopPostClearGlow();
        popUps.ShowWinScreen();
        SaveLevelProgress();
    }

    /// <summary>
    /// Stops the post-clear glow effect on the progress bar and opens the lose screen.
    /// </summary>
    public void OpenLoseScreen()
    {
        progressBarManager.StopPostClearGlow();
        popUps.ShowLoseScreen();
    }

    /// <summary>
    /// Stops the post-clear glow effect on the progress bar, updates the current level,
    /// and loads the next level scene.
    /// </summary>
    public void GoToTheNextLevel()
    {
        progressBarManager.StopPostClearGlow();
        currentLevel++;
        SceneController.Instance.LoadLevelScene();
    }

    /// <summary>
    /// Decreases the target count for the specified target type by the number of destroyed targets.
    /// </summary>
    /// <param name="currTarget">The target Game Object to decrease the count of.</param>
    /// <param name="numDestroyed">The amount to decrease the taget count by.</param>
    public void DecreaseTargetCount(GameObject currTarget, int numDestroyed)
    {
        // Get the string representation of the target type
        string targetType = ChangeTargetTypeToString(currTarget);

        // Get the remaining count for the target type and update it, if there is at least one remaining
        if (currentTargetCounts.TryGetValue(targetType, out int remaining) && remaining > 0)
        {
            // Calculate the new count, ensuring it doesn't go below zero
            int newCount = Mathf.Max(0, remaining - numDestroyed);
            // Calculate the actual number of targets destroyed
            int actualDestroyed = remaining - newCount;

            // Update the current target counts
            currentTargetCounts[targetType] = newCount;
            destroyedTargetCount += actualDestroyed;

            // Update the UI for the target count and progress bar
            targetsUIManager.UpdateTargetCount(targetType, newCount);

            // Update the progress bar based on the destroyed targets
            float progress = (float)destroyedTargetCount / totalTargetCount;
            progressBarManager.SetProgress(progress);
        }
    }

    /// <summary>
    /// Returns a string representation of the target type based on the GameObject's component type.
    /// </summary>
    /// <param name="currTarget">The target Game Object to find the string of.</param>
    private string ChangeTargetTypeToString(GameObject currTarget)
    {
        // Check if the GameObject has a Cube component and return the corresponding string
        if (currTarget.TryGetComponent(out Cube cube))
            return ChangeCubeColorToString(cube.GetColor());
        // Check if the GameObject has an Obstacle component and return the corresponding string
        if (currTarget.TryGetComponent(out Obstacle obs))
            return ChangeObstacleTypeToString(currTarget.name.Replace("(Clone)", "").Trim());
        return null;
    }

    /// <summary>
    /// Returns a string representation of the cube's color.
    /// </summary>
    /// <param name="cubeColor">The color of the cube.</param>
    private string ChangeCubeColorToString(CubeColor cubeColor) => cubeColor switch
    {
        CubeColor.Red => "R",
        CubeColor.Blue => "B",
        CubeColor.Yellow => "Y",
        CubeColor.Black => "BL",
        CubeColor.White => "W",
        _ => null
    };

    /// <summary>
    /// Returns a string representation of the Obstacle type.
    /// </summary>
    /// <param name="targetType">The type of the target Obstacle.</param>
    private string ChangeObstacleTypeToString(string targetType) => targetType switch
    {
        "PrismObstacle" => "P",
        "StoneObstacle" => "S",
        _ => null
    };

    /// <summary>
    /// Saves the level progress by checking if the current level is the highest unlocked level.
    /// </summary>
    public void SaveLevelProgress()
    {
        // Get the highest unlocked level from PlayerPrefs, defaulting to 1 if not set
        int highestUnlocked = PlayerPrefs.GetInt("HighestUnlockedLevel", 1);

        // If the current level number is greater than or equal to the highest unlocked level,
        if (levelNumber >= highestUnlocked)
        {
            // Update the highest unlocked level to the next level and save it
            PlayerPrefs.SetInt("HighestUnlockedLevel", levelNumber + 1);
            PlayerPrefs.Save();
        }
    }
}
