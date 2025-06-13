using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Animates a Chain Lightning effect from the Special Yellow Cube
/// using a sequence of textures on a LineRenderer.
/// Automatically despawns itself after a set duration.
/// </summary>
public class ChainLine : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Array of lightning textures to cycle through for the zap effect.")]
    public Texture[] frames;
    [Tooltip("Duration each frame is shown before switching to the next.")]
    public float frameDuration = 0.05f;

    private LineRenderer lr;
    private int currentFrame = 0;
    private float timer = 0f;

    /// <summary>
    /// Initializes the texture animation and starts the auto-despawn timer.
    /// </summary>
    void OnEnable()
    {
        currentFrame = 0;
        timer = 0f;

        // Change the lightning texture to give the zapping effect
        if (frames.Length > 0)
        {
            lr.material.mainTexture = frames[0];
        }
        // Despawn it after the effect is done, duration is 2 seconds
        StartCoroutine(AutoDespawn(2f));
    }

    /// <summary>
    /// Gets the LineRenderer component for the zap.
    /// </summary>
    void Awake()
    {
        lr = GetComponent<LineRenderer>();
    }

    /// <summary>
    /// Updates the texture animation based on the frame duration.
    /// </summary>
    void Update()
    {
        if (frames.Length <= 1 || lr == null) return;

        timer += Time.deltaTime;
        if (timer >= frameDuration)
        {
            timer = 0f;
            currentFrame = (currentFrame + 1) % frames.Length;
            lr.material.mainTexture = frames[currentFrame];
        }
    }

    /// <summary>
    /// Despawns this object after a fixed delay using object pooling.
    /// </summary>
    private IEnumerator AutoDespawn(float delay)
    {
        yield return new WaitForSeconds(delay);
        ObjectPoolManager.DespawnObject(this.gameObject);
    }
}
