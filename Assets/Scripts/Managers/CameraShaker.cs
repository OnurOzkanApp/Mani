using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles camera shake effects using a singleton pattern. 
/// Useful for adding screen feedback during gameplay events (e.g., explosions, combos).
/// </summary>
public class CameraShaker : MonoBehaviour
{
    // Singleton instance reference for Camera Shaker
    public static CameraShaker Instance;

    // Original position of the camera to reset after shaking
    private Vector3 originalPosition;
    // Flag to check if the camera is currently shaking
    private bool isShaking = false;

    /// <summary>
    /// Checks if the instance already exists, if not, sets this as the instance.
    /// If an instance already exists, destroys this GameObject to avoid duplicates.
    /// Resets the score to zero at the start.
    /// </summary>
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
        // Store the original position of the camera at the start
        originalPosition = Camera.main.transform.localPosition;
    }

    /// <summary>
    /// Shakes the camera for a specified duration and magnitude using a coroutine.
    /// </summary>
    /// <param name="duration">The duration of the shake.</param>
    /// <param name="magnitude">The strength or the magnitude of the shake.</param>
    public void Shake(float duration, float magnitude)
    {
        // Check if the camera is already shaking to avoid starting a new shake while one is in progress
        if (!isShaking)
        {
            // If not shaking, start the shake coroutine
            StartCoroutine(ShakeCoroutine(duration, magnitude));
        }
    }

    /// <summary>
    /// Shakes the camera for a specified duration and magnitude using a coroutine.
    /// </summary>
    /// <param name="duration">The duration of the shake.</param>
    /// <param name="magnitude">The strength or the magnitude of the shake.</param>
    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        // Set the camera as shaking at the start of the coroutine to avoid re-entrancy
        isShaking = true;
        float elapsed = 0f;

        // Shake the camera by randomly changing its position within the specified magnitude for the duration
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            Camera.main.transform.localPosition = originalPosition + new Vector3(x, y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Reset the camera position to the original position after shaking
        Camera.main.transform.localPosition = originalPosition;
        // Update the isShaking flag to false to indicate that shaking has ended
        isShaking = false;
    }
}