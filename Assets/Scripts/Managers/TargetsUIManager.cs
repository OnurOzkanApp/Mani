using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TargetsUIManager : MonoBehaviour
{
    // Internal dictionary to map target identifier strings to sprites
    private Dictionary<string, Sprite> targetTypeToSprite;

    // After initializing the targets, keep track of the targets during runtime using a Dictionary
    private Dictionary<string, GameObject> targetUIElements = new Dictionary<string, GameObject>();

    [Header("UI References")]
    [Tooltip("Parent container for the Target UI elements")]
    [SerializeField] private Transform targetsParentObject;
    [Tooltip("Prefab for a single target display")]
    [SerializeField] private GameObject targetUIPrefab;

    [Header("Cube Sprites Mapping")]
    [Tooltip("Sprite for Black cube (use cube identifier 'BL')")]
    [SerializeField] private Sprite blackCubeSprite;
    [Tooltip("Sprite for Blue cube (use cube identifier 'B')")]
    [SerializeField] private Sprite blueCubeSprite;
    [Tooltip("Sprite for Red cube (use cube identifier 'R')")]
    [SerializeField] private Sprite redCubeSprite;
    [Tooltip("Sprite for Yellow cube (use cube identifier 'Y')")]
    [SerializeField] private Sprite yellowCubeSprite;
    [Tooltip("Sprite for White cube (use cube identifier 'W')")]
    [SerializeField] private Sprite whiteCubeSprite;

    [Header("Obstacle Sprites Mapping")]
    [Tooltip("Sprite for Prism (use obstacle identifier 'P')")]
    [SerializeField] private Sprite prismSprite;
    [Tooltip("Sprite for Stone (use obstacle identifier 'S')")]
    [SerializeField] private Sprite stoneSprite;

    /// <summary>
    /// Initializes the target type to sprite mapping dictionary.
    /// </summary>
    private void Awake()
    {
        targetTypeToSprite = new Dictionary<string, Sprite>()
        {
            {"BL", blackCubeSprite},
            {"B", blueCubeSprite},
            {"R", redCubeSprite},
            {"Y", yellowCubeSprite},
            {"W", whiteCubeSprite  },
            {"P", prismSprite  },
            {"S", stoneSprite  }
        };
    }

    /// <summary>
    /// Retrieves the current level targets and initializes the UI at the start.
    /// </summary>
    private void Start()
    {
        // Get the targets for the current level from the Level Manager
        var targets = LevelManager.Instance.GetLevelTargets();

        // Populate UI based on the targets
        CreateTargetUIElements(targets);
    }

    // <summary>
    /// Instantiates and sets up the target UI elements based on the current level's target data.
    /// Clears previous UI elements, positions new ones, assigns sprites, and displays remaining target counts.
    /// </summary>
    /// <param name="targets">An array of target data (type and count) loaded from the current level's JSON.</param>
    private void CreateTargetUIElements(JsonReader.LevelTarget[] targets)
    {
        // If the object that the targets are in is not empty, clear previous targets
        if (targetsParentObject.childCount != 0)
        {
            // Destroy all existing target UI elements by going through each child of the targets parent object
            foreach (Transform targetTransform in targetsParentObject)
            {
                Destroy(targetTransform);
            }
            // Clear the dictionary of target UI elements
            targetUIElements.Clear();
        }

        // Get the target positions based on the number of targets
        List<Vector2> targetPositions = AdjustTargetPositions(targets);
        // Set the starting index for the loop
        int i = 0;

        // Go through each target in the targets array
        foreach (JsonReader.LevelTarget target in targets)
        {
            // Instantiate a new target UI element from the prefab
            GameObject newTargetGO = Instantiate(targetUIPrefab, targetsParentObject);
            RectTransform rectTransform = newTargetGO.GetComponent<RectTransform>();

            // Position the target
            rectTransform.anchoredPosition = targetPositions[i];

            // Apply scaling to the target based on number of targets
            if (targets.Length >= 5)
            {
                rectTransform.localScale = new Vector2(0.6f, 0.6f);
            }
            else
            {
                rectTransform.localScale = new Vector2(0.7f, 0.7f);
            }

            // Update icon sprite and the remaining count number for the target and the remaining count text respectively
            Image targetImage = newTargetGO.GetComponentInChildren<Image>();
            TMP_Text remainingCountText = newTargetGO.GetComponentInChildren<TMP_Text>();

            if (targetTypeToSprite.TryGetValue(target.targetType, out Sprite sprite))
            {
                targetImage.sprite = sprite;
            }
            else
            {
                Debug.LogWarning($"Sprite not found for cube type: {target.targetType}");
            }

            remainingCountText.text = target.count.ToString();
            targetUIElements[target.targetType] = newTargetGO;
            i++;
        }
    }

    // <summary>
    /// Updates the Remaining Target text given the type of the target and the updated count.
    /// </summary>
    /// <param name="targetType">A string of target type.</param>
    /// <param name="newCount">The updated number of the target.</param>
    public void UpdateTargetCount(string targetType, int newCount)
    {
        // Get the GameObject for the target type from the dictionary if it exists
        if (targetUIElements.TryGetValue(targetType, out GameObject targetUI))
        {
            // Get the TMP_Text component that displays the count
            TMP_Text countText = targetUI.GetComponentInChildren<TMP_Text>();
            // Check if the text exists
            if (countText != null)
            {
                // If so, change the number of the remaining target to the new count
                countText.text = newCount.ToString();
            }
        }
    }

    // <summary>
    /// Adjusts the target positions based on the number of given targets.
    /// </summary>
    /// <param name="targets">An array of target data (type and count) loaded from the current level's JSON.</param>
    private List<Vector2> AdjustTargetPositions(JsonReader.LevelTarget[] targets)
    {
        // Create a list to hold the target positions
        List<Vector2> targetPositions = new List<Vector2>();
        // Define the spacing between targets based on the number of targets
        float spacing = 60f;

        // Check the number of targets and adjust the positions accordingly
        if (targets.Length == 1)
        {
            targetPositions.Add(new Vector2(0, 0));
        }
        else if (targets.Length == 2)
        {
            targetPositions.Add(new Vector2(-spacing, 0));
            targetPositions.Add(new Vector2(spacing, 0));
        }
        else if (targets.Length == 3)
        {
            // After 3 targets, increase the spacing to avoid overlap
            spacing = 100f;

            targetPositions.Add(new Vector2(-spacing, 0));
            targetPositions.Add(new Vector2(spacing, 0));
            targetPositions.Add(new Vector2(0, 0));
        }
        else if (targets.Length == 4)
        {
            // Adjust the spacing for 4 targets to ensure they fit well
            spacing = 80f;

            targetPositions.Add(new Vector2(-spacing * 1.5f, 0));
            targetPositions.Add(new Vector2(-0.5f * spacing, 0));
            targetPositions.Add(new Vector2(0.5f * spacing, 0));
            targetPositions.Add(new Vector2(1.5f*spacing, 0));
        }
        else if (targets.Length == 5)
        {
            // Adjust the spacing for 5 targets to ensure they fit well
            spacing = 70f;

            targetPositions.Add(new Vector2(-spacing * 2f, 0));
            targetPositions.Add(new Vector2(-spacing, 0));
            targetPositions.Add(new Vector2(0, 0));
            targetPositions.Add(new Vector2(spacing, 0));
            targetPositions.Add(new Vector2(2f * spacing, 0));

        }
        else if (targets.Length == 6)
        {
            spacing = 32f;
            targetPositions.Add(new Vector2(-spacing * 5f, 0));
            targetPositions.Add(new Vector2(-spacing * 3f, 0));
            targetPositions.Add(new Vector2(-spacing, 0));
            targetPositions.Add(new Vector2(spacing, 0));
            targetPositions.Add(new Vector2(3f * spacing, 0));
            targetPositions.Add(new Vector2(5f * spacing, 0));

        }
        // Then, return the list of target positions
        return targetPositions;
    }

}
