using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    // Singleton instance reference for Scene Controller
    public static SceneController Instance { get; private set; }

    // Store the name of the previous scene to allow returning to it
    private static string previousScene;

    /// <summary>
    /// Checks if the instance already exists, if not, sets this as the instance.
    /// If an instance already exists, logs a warning and destroys this GameObject to avoid duplicates.
    /// </summary>
    private void Awake()
    {
        if (SceneController.Instance == null)
        {
            SceneController.Instance = this;
        }
        else
        {
            Debug.LogWarning("Duplicate SceneController found.");
            Destroy(gameObject);
            return;
        }
#if UNITY_STANDALONE
        // For PC, Mac, and Linux standalone builds only
        Screen.SetResolution(608, 1080, false);
#endif
    }

    /// <summary>
    /// Loads a scene by its name.
    /// </summary>
    /// <param name="sceneName">The name of the scene to load.</param>
    public void LoadSceneByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Loads the Main Scene, despawning all board objects first.
    /// </summary>
    public void LoadMainScene()
    {
        GameBoard.Instance.DespawnAllBoardObjects();
        SceneManager.LoadScene("MainScene");
    }

    /// <summary>
    /// Loads the Level Scene, storing the current scene name as the previous scene.
    /// </summary>
    public void LoadLevelScene()
    {
        previousScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene("LevelScene");
    }

    /// <summary>
    /// Retrieves the name of the current active scene.
    /// </summary>
    public string GetCurrentSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }

    /// <summary>
    /// Retrieves the name of the previous scene that was loaded before the current one.
    /// </summary>
    public static string GetPreviousSceneName()
    {
        return previousScene;
    }
}
