using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadMenuManager : MonoBehaviour
{
    [Header("Load Menu Base Panel")]
    [Tooltip("Load Menu Panel to inactivate when closing the Load Menu.")]
    [SerializeField] private GameObject loadMenuBase;

    [Header("Level Button UI")]
    [Tooltip("Prefab used to instantiate level selection buttons.")]
    [SerializeField] private GameObject levelButtonPrefab;
    [Tooltip("Parent container (e.g., a ScrollView Content) where level buttons will be instantiated.")]
    [SerializeField] private Transform contentHolder;

    [Header("Button Sprites")]
    [Tooltip("Sprite used for unlocked levels.")]
    [SerializeField] private Sprite unlockedSprite;
    [Tooltip("Sprite used for locked levels.")]
    [SerializeField] private Sprite lockedSprite;

    /// <summary>
    /// Creates level buttons for each level from 1 to 50 based on the highest unlocked level stored in PlayerPrefs.
    /// </summary>
    void Start()
    {
        // Get the highest unlocked level from PlayerPrefs, defaulting to 1 if not set
        int highestUnlockedLevel = PlayerPrefs.GetInt("HighestUnlockedLevel", 1);

        // Number of available levels
        int totalLevels = JsonReader.Instance.GetTotalLevelCount();

        // Create buttons for each level
        for (int i = 1; i <= totalLevels; i++)
        {
            // Instantiate a new button for the level
            GameObject buttonGO = Instantiate(levelButtonPrefab, contentHolder);
            // Get the Button, Image, and Text components from the instantiated button
            Button button = buttonGO.GetComponent<Button>();
            Image image = buttonGO.GetComponent<Image>();
            TMP_Text text = buttonGO.GetComponentInChildren<TMP_Text>();

            // If the current level is less than or equal to the highest unlocked level
            if (i <= highestUnlockedLevel)
            {
                // Unlocked level
                image.sprite = unlockedSprite;
                button.interactable = true;
                text.text = $"LEVEL\n{i}";
                int levelIndex = i - 1; // 0 based index for LevelManager
                button.onClick.AddListener(() => LoadLevel(levelIndex));
            }
            // If the player has not unlocked this level yet
            else
            {
                // Locked level
                image.sprite = lockedSprite;
                button.interactable = false;
            }
        }
    }

    /// <summary>
    /// Loads the level scene based on the index provided.
    /// </summary>
    void LoadLevel(int index)
    {
        // Change the level index in LevelManager and load the level scene
        LevelManager.SetLevelIndex(index);
        SceneController.Instance.LoadLevelScene();
    }

    /// <summary>
    /// CLoses the Load Menu to return to the Main Menu by deactivating the Settings Panel.
    /// </summary>
    public void CloseLoadMenu()
    {
        loadMenuBase.SetActive(false);
    }
}
