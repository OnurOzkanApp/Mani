using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Singleton manager responsible for tracking and displaying combo counts,
/// as well as triggering visual feedback (e.g., progress bar shake, combo text).
/// </summary>
public class ComboManager : MonoBehaviour
{
    public static ComboManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Transform comboTextParent;
    [SerializeField] private ProgressBarManager progressBarManager;

    private int currentComboCount = 0;

    /// <summary>
    /// Checks if the instance already exists, if not, sets this as the instance.
    /// If an instance already exists, destroys this GameObject to avoid duplicates.
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// Returns the current combo count.
    /// </summary>
    public int GetComboCount()
    {
        return currentComboCount;
    }

    /// <summary>
    /// Resets the current combo count to zero.
    /// </summary>
    public void ResetCombo()
    {
        currentComboCount = 0;
    }

    /// <summary>
    /// Increases the current combo count by one.
    /// </summary>
    public void IncrementCombo()
    {
        currentComboCount++;
    }

    /// <summary>
    /// Finds the position of the cube in world space and spawns a combo text above it.
    /// </summary>
    /// <param name="cubeTransform">The cube to show the combo text above.</param>
    public void ShowComboTextAt(Transform cubeTransform)
    {
        // If the combo count is less than 2, do not show the combo text
        if (currentComboCount < 2) return;

        // Find the position of the cube in world space
        Vector3 screenPos = Camera.main.WorldToScreenPoint(cubeTransform.position);
        GameObject comboGO = ObjectPoolManager.SpawnObjectByKey("ComboText", screenPos, false);
        comboGO.transform.SetParent(comboTextParent, false);

        // Set the position of the combo text in the UI
        RectTransform canvasRect = comboTextParent as RectTransform;
        RectTransform comboRect = comboGO.GetComponent<RectTransform>();

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            null,
            out Vector2 localPoint))
        {
            comboRect.anchoredPosition = localPoint;
        }

        // Get the TMP_Text component and set the combo count text
        TMP_Text numberText = comboGO.transform.Find("ComboNumberText").GetComponent<TMP_Text>();
        // If the combo numbert text exists, update it with the current combo count
        if (numberText != null)
        {
            numberText.text = currentComboCount.ToString();
        }

        // If all targets are destroyed, shake the progress bar related to the combo count
        if (LevelManager.Instance.CheckIfAllTargetsAreDestroyed())
        {
            float intensity = 5f + (currentComboCount * 3f);
            progressBarManager.ShakeBar(0.15f, intensity);
        }
        // Start the animation and despawn coroutine for the combo text
        StartCoroutine(AnimateAndDespawn(comboGO));
    }

    /// <summary>
    /// Animates the combo text and despawns it after a delay.
    /// </summary>
    /// <param name="comboGO">The Game Object of the combo text.</param>
    private IEnumerator AnimateAndDespawn(GameObject comboGO)
    {
        // Get the RectTransform and CanvasGroup components of the combo text GameObject
        RectTransform rectTransform = comboGO.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = comboGO.GetComponent<CanvasGroup>();

        // If the CanvasGroup component does not exist, add it to the combo text GameObject
        if (canvasGroup == null) canvasGroup = comboGO.AddComponent<CanvasGroup>();

        // Set the initial scale to zero
        Vector3 startScale = Vector3.zero;

        // Controlled scaling per combo level
        // Base scale is 1, max scale is 2.2, and scale step is 0.05
        float baseScale = 1f;
        float maxScale = 2.2f;
        float scaleStep = 0.05f;

        // Calculate the final scale based on the current combo count
        float finalScale = Mathf.Min(baseScale + (currentComboCount - 2) * scaleStep, maxScale);
        // Change the pop scale of the combo text proportionally to the final scale
        Vector3 popScale = Vector3.one * finalScale;

        // Set the start position and end position for the combo text animation
        Vector3 startPos = rectTransform.localPosition;
        Vector3 endPos = startPos + new Vector3(0, 40f, 0);

        // Define the animation durations
        float scaleTime = 0.2f;
        float floatTime = 0.8f;
        float fadeDelay = 0.2f;

        float elapsed = 0f;
        // Animate the pop effect by scaling the combo text up
        while (elapsed < scaleTime)
        {
            float t = elapsed / scaleTime;
            comboGO.transform.localScale = Vector3.Lerp(startScale, popScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        // Ensure the final scale is set
        comboGO.transform.localScale = Vector3.one * finalScale;

        // Float upward and fade out
        elapsed = 0f;
        canvasGroup.alpha = 1f;

        while (elapsed < floatTime)
        {
            float t = elapsed / floatTime;
            rectTransform.localPosition = Vector3.Lerp(startPos, endPos, t);
            if (t > fadeDelay)
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, (t - fadeDelay) / (1f - fadeDelay));
            elapsed += Time.deltaTime;
            yield return null;
        }
        // Ensure the final alpha is set
        canvasGroup.alpha = 0f;
        // Despawn the combo text object
        ObjectPoolManager.DespawnObject(comboGO);
    }
}

