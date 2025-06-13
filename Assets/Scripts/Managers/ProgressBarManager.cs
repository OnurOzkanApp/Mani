using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the game's progress bar UI, including animations for normal fills,
/// glow effects after clearing targets, and screen shake feedback.
/// </summary>
public class ProgressBarManager : MonoBehaviour
{
    [Header("Progress Bar Root")]
    [Tooltip("Root object that holds the progress bar and is shaken for visual feedback.")]
    [SerializeField] private Transform progressBarRoot;

    [Header("Progress Fill and Glow")]
    [Tooltip("Image component representing the fill of the progress bar.")]
    [SerializeField] private Image fillBarImage;
    [Tooltip("Image used for visual glow effects during progress events.")]
    [SerializeField] private Image glowEffectImage;

    [Header("Progress Fill Variants")]
    [Tooltip("GameObject shown before clearing all targets.")]
    [SerializeField] private GameObject preClearFillBar;
    [Tooltip("GameObject shown after clearing all targets.")]
    [SerializeField] private GameObject postClearFillBar;

    // Internal coroutine tracking
    private Coroutine continuousGlowCoroutine;

    // Fill value target (0.0 to 1.0)
    private float targetFill = 0f;

    // Glow colors for various states
    private readonly Color normalGlowStart = new Color(1f, 0.6f, 0.1f, 0.8f); // orange
    private readonly Color normalGlowEnd = new Color(1f, 0.6f, 0.1f, 0f);

    private readonly Color postClearGlowStart = new Color(0f, 1f, 1f, 0.9f);  // cyan
    private readonly Color postClearGlowEnd = new Color(0f, 1f, 1f, 0f);

    /// <summary>
    /// Sets the progress bar fill at the beginning of the level.
    /// </summary>
    public void SetProgressAtTheStart(float normalizedValue)
    {
        targetFill = Mathf.Clamp01(normalizedValue);
        StopAllCoroutines();
        StartCoroutine(AnimateFill());
    }

    /// <summary>
    /// Updates the progress bar fill and triggers a glow pulse.
    /// </summary>
    public void SetProgress(float normalizedValue)
    {
        targetFill = Mathf.Clamp01(normalizedValue);
        StopAllCoroutines();
        StartCoroutine(AnimateFill());
        StartCoroutine(GlowPulse());
    }

    /// <summary>
    /// Smoothly animates the fill amount of the progress bar.
    /// </summary>
    private IEnumerator AnimateFill()
    {
        float duration = 0.3f;
        float elapsed = 0f;
        float startFill = fillBarImage.fillAmount;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            fillBarImage.fillAmount = Mathf.Lerp(startFill, targetFill, EaseOutQuad(t));
            yield return null;
        }

        fillBarImage.fillAmount = targetFill;
    }

    /// <summary>
    /// Easing function for smoother fill animation.
    /// </summary>
    private float EaseOutQuad(float t)
    {
        return t * (2f - t);
    }

    /// <summary>
    /// Emits a brief glow pulse when progress is made (normal state).
    /// </summary>
    private IEnumerator GlowPulse()
    {
        if (glowEffectImage == null) yield break;

        glowEffectImage.color = normalGlowStart;
        yield return new WaitForSeconds(0.3f);
        glowEffectImage.color = normalGlowEnd;
    }

    public void TriggerPostClearGlow()
    {
        StopAllCoroutines();

        preClearFillBar.SetActive(false);
        postClearFillBar.SetActive(true);

        if (continuousGlowCoroutine != null)
            StopCoroutine(continuousGlowCoroutine);

        continuousGlowCoroutine = StartCoroutine(ContinuousPostClearGlow());
    }

    /// <summary>
    /// Continuously pulses the progress bar glow after all targets are cleared.
    /// </summary>
    private IEnumerator ContinuousPostClearGlow()
    {
        if (glowEffectImage == null) yield break;

        float duration = 1f;

        while (true)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float alpha = Mathf.Lerp(0f, postClearGlowStart.a, t / duration);
                glowEffectImage.color = new Color(postClearGlowStart.r, postClearGlowStart.g, postClearGlowStart.b, alpha);
                yield return null;
            }

            t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float alpha = Mathf.Lerp(postClearGlowEnd.a, 0f, t / duration);
                glowEffectImage.color = new Color(postClearGlowEnd.r, postClearGlowEnd.g, postClearGlowEnd.b, alpha);
                yield return null;
            }
        }
    }


    /// <summary>
    /// Triggers a stronger glow pulse effect when extra matches are made after all targets are cleared.
    /// </summary>
    public void TriggerChargedPulse()
    {
        if (glowEffectImage == null) return;

        StopCoroutine(nameof(GlowPulse));
        StartCoroutine(ChargedGlowPulse());
    }

    /// <summary>
    /// Stronger pulse effect animation for charged feedback.
    /// </summary>
    private IEnumerator ChargedGlowPulse()
    {
        glowEffectImage.color = postClearGlowStart;
        yield return new WaitForSeconds(0.4f);

        float fadeDuration = 0.4f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            glowEffectImage.color = new Color(postClearGlowStart.r, postClearGlowStart.g, postClearGlowStart.b, alpha);
            yield return null;
        }

        glowEffectImage.color = Color.clear;
    }

    /// <summary>
    /// Stops any ongoing glow effect and resets the glow color.
    /// </summary>
    public void StopPostClearGlow()
    {
        if (continuousGlowCoroutine != null)
        {
            StopCoroutine(continuousGlowCoroutine);
            continuousGlowCoroutine = null;
        }

        if (glowEffectImage != null)
        {
            glowEffectImage.color = Color.clear;
        }
    }

    /// <summary>
    /// Triggers a shake animation of the entire progress bar UI.
    /// </summary>
    public void ShakeBar(float duration = 0.15f, float intensity = 5f)
    {
        StartCoroutine(ShakeBarRoutine(duration, intensity));
    }

    /// <summary>
    /// Coroutine to shake the progress bar for feedback effect.
    /// </summary>
    private IEnumerator ShakeBarRoutine(float duration, float intensity)
    {
        Vector3 originalPos = progressBarRoot.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float offsetX = Random.Range(-1f, 1f) * intensity;
            float offsetY = Random.Range(-1f, 1f) * intensity;

            progressBarRoot.localPosition = originalPos + new Vector3(offsetX, offsetY, 0f);
            yield return null;
        }

        progressBarRoot.localPosition = originalPos;
    }
}