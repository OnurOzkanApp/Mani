using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls the grow, rotate, and pulse animation of the Magical Sicil VFX.
/// Despawns itself automatically after completing the animation.
/// </summary>
public class MagicSicilVFX : MonoBehaviour
{
    [Header("Timings")]
    [Tooltip("Time it takes to grow the VFX from 0 to max scale.")]
    [SerializeField] private float growDuration = 0.5f;
    [Tooltip("Time to complete one full rotation after growth.")]
    [SerializeField] private float rotateDuration = 0.7f;
    [Tooltip("Duration of the pulsing glow effect after rotation.")]
    [SerializeField] private float pulseDuration = 0.6f;

    [Header("Scaling & Curves")]
    [Tooltip("Maximum scale the VFX grows to.")]
    [SerializeField] private float maxScale = 1f;
    [Tooltip("Animation curve to control scale growth.")]
    [SerializeField] private AnimationCurve scaleCurve;
    [Tooltip("Animation curve for sprite transparency during pulse.")]
    [SerializeField] private AnimationCurve alphaCurve;

    [Header("Visuals")]
    [Tooltip("Main sprite renderer used to change color and glow.")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("Black Smoke Particle System.")]
    [SerializeField] private ParticleSystem blackSmoke;
    [Tooltip("Reference to the rotating visual transform.")]
    [SerializeField] private Transform magicSicilSprite;

    // The elapse time and the initial rotation of the Magic Sicil
    private float elapsed = 0f;
    private Quaternion initialRotation;

    /// <summary>
    /// Stores the initial rotation and ensures required child objects are assigned.
    /// </summary>
    void Awake()
    {
        initialRotation = transform.rotation;

        if (magicSicilSprite == null)
            magicSicilSprite = transform.Find("RotatingVisual");

        if (blackSmoke == null)
            blackSmoke = GetComponentInChildren<ParticleSystem>();
    }

    /// <summary>
    /// Resets the visual state and starts the animation when the object is enabled.
    /// </summary>
    void OnEnable()
    {
        // Reset the elapse time and the initial rotation
        elapsed = 0f;
        transform.rotation = initialRotation;

        // Reset the rotation and the scale of the Magic Sicil
        if (magicSicilSprite != null)
        {
            magicSicilSprite.localRotation = Quaternion.identity;
            magicSicilSprite.localScale = Vector3.zero;
        }

        // Make sure the Sicil is fully visible
        if (spriteRenderer != null)
            spriteRenderer.color = new Color(1f, 1f, 1f, 1f);

        // Clear the Black Smoke if it exists currently, then play it again
        if (blackSmoke != null)
        {
            blackSmoke.Clear(true);
            blackSmoke.Play();
        }
    }

    /// <summary>
    /// Runs the animation lifecycle of Magic Sicil.
    /// Rotates, pulses, and then despawns.
    /// </summary>
    void Update()
    {
        elapsed += Time.deltaTime;

        // Grow the Sicil while rotating
        if (elapsed <= growDuration)
        {
            float t = elapsed / growDuration;
            float scale = scaleCurve.Evaluate(t) * maxScale;
            magicSicilSprite.localScale = Vector3.one * scale;
            magicSicilSprite.Rotate(0f, 0f, 180f * Time.deltaTime);
        }
        // Lock in the Sicil
        else if (elapsed <= growDuration + rotateDuration)
        {
            float t = (elapsed - growDuration) / rotateDuration;
            magicSicilSprite.localScale = Vector3.one * maxScale;
            magicSicilSprite.localRotation = Quaternion.Euler(0f, 0f, 360f * t);
        }
        // Pulse the Sicil
        else if (elapsed <= growDuration + rotateDuration + pulseDuration)
        {
            float t = (elapsed - growDuration - rotateDuration) / pulseDuration;
            float pulse = 1f + 0.2f * Mathf.Sin(t * Mathf.PI);
            magicSicilSprite.localScale = Vector3.one * maxScale * pulse;

            if (spriteRenderer != null)
            {
                float glow = Mathf.Sin(t * Mathf.PI);
                Color start = new Color(0.5f, 0f, 1f, 1f);
                Color end = new Color(1f, 0.3f, 1f, 1f);
                spriteRenderer.color = Color.Lerp(start, end, glow);
            }
        }
        // Then despawn the Magic Sicil at the end
        else
        {
            ObjectPoolManager.DespawnObject(this.gameObject);
        }
    }
}
