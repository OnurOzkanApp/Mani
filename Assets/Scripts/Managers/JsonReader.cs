using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JsonReader : MonoBehaviour
{
    // Singleton instance of JsonReader
    public static JsonReader Instance { get; private set; }

    // List to hold all JSON data files
    public List<TextAsset> jsonDataList;
    // List to hold all levels parsed from JSON files
    private List<Level> allLevels = new List<Level>();

    /// <summary>
    /// Checks if the instance already exists, if not, sets this as the instance.
    /// If an instance already exists, logs a warning and destroys this GameObject
    /// to avoid duplicates.
    /// </summary>
    private void Awake()
    {
        // Check if the instance already exists
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Duplicate JsonReader found.");
            Destroy(gameObject);
            return;
        }

        // Load all level data from JSON files
        foreach (TextAsset file in jsonDataList)
        {
            // Deserialize each JSON file into a Level object and add it to the allLevels list
            Level level = JsonUtility.FromJson<Level>(file.text);
            allLevels.Add(level);
        }

    }

    /// <summary>
    /// Returns a list of all levels loaded from JSON.
    /// </summary>
    public List<Level> GetAllLevels()
    {
        return allLevels;
    }


    /// <summary>
    /// Returns the total number of levels loaded from JSON.
    /// </summary>
    public int GetTotalLevelCount()
    {
        if (allLevels == null || allLevels.Count == 0)
        {
            Debug.LogError("No levels found in the JSON data.");
            return 0;
        }
        return allLevels.Count;
    }

    [Serializable]
    public class Level
    {
        // Number of Level, Grid Width, Height, and Number of Moves available as int
        public int level_number;
        public int grid_width;
        public int grid_height;
        public int move_count;
        // List of board objects as strings to initialize the board at the beginning of the level
        public List<string> board;
        // Array of target objectives for the level (using an array here since it will have just one type of object inside)
        public LevelTarget[] targets;
    }

    [Serializable]
    public class LevelTarget
    {
        // Represents the target type (e.g., "R" for Red Cube, "B" for Blue Cube, etc.)
        public string targetType;
        // Number of targets to clear
        public int count;
    }
}
