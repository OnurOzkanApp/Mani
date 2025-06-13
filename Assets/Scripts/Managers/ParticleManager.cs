using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Color = UnityEngine.Color;

/// <summary>
/// Controls all visual and sound effects related to cube destruction, special abilities,
/// screen overlays, camera shake, and audio cues.
/// Utilizes object pooling and coroutine-based timing for performance and polish.
/// </summary>
public class ParticleManager : MonoBehaviour
{
    // Singleton instance for ParticleManager
    public static ParticleManager Instance;

    // Dictionary to hold particle effects for cube destruction based on color and number of matches
    private Dictionary<(CubeColor, int), GameObject> cubeDestructionEffects;
    // Dictionary to hold particle effects for Special Cube destruction based on color
    private Dictionary<CubeColor, GameObject> specialCubeDestructionEffects;

    [Header("Visual & Audio State")]
    // Flag to track if any effect is currently active
    private bool isEffectActive = false;
    // Flag to track if a five-plus effect has been played
    private bool hasPlayedFivePlusEffect = false;
    private MaterialPropertyBlock rippleBlock;
    private SpriteRenderer backgroundRenderer;

    [Header("References")]
    [Tooltip("Parent container for all active particles.")]
    [SerializeField] private Transform particlesParent;
    [Tooltip("Background image to apply ripple effects.")]
    [SerializeField] private GameObject background;
    [Tooltip("Camera shaker for screen feedback.")]
    [SerializeField] private CameraShaker cameraShaker;

    [Header("Screen Overlays")]
    [Tooltip("White overlay image for bright flash effects.")]
    [SerializeField] private GameObject whiteScreenGlow;
    [Tooltip("White cube ultimate visual effect.")]
    [SerializeField] private GameObject ultimateWhiteGlowVFX;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip evilLaughClip;
    [SerializeField] private AudioClip waterfallClip;
    [SerializeField] private AudioClip meteorClip;
    [SerializeField] private AudioClip explosionClip;
    [SerializeField] private AudioClip thunderClip;
    [SerializeField] private AudioClip rippleClip;

    [Header("4 Cube Destruction Effects")]
    [SerializeField] private GameObject fourBlackCubeEffect;
    [SerializeField] private GameObject fourBlueCubeEffect;
    [SerializeField] private GameObject fourRedCubeEffect;
    [SerializeField] private GameObject fourYellowCubeEffect;

    [Header("5+ Cube Destruction Effects")]
    [SerializeField] private GameObject fiveBlackCubeEffect;
    [SerializeField] private GameObject fiveBlueCubeEffect;
    [SerializeField] private GameObject fiveRedCubeEffect;
    [SerializeField] private GameObject fiveYellowCubeEffect;

    [Header("Special Cube Destruction Effects")]
    [SerializeField] private GameObject specialBlackCubeEffect;
    [SerializeField] private GameObject specialBlueCubeEffect;
    [SerializeField] private GameObject specialRedCubeEffect;
    [SerializeField] private GameObject specialYellowCubeEffect;

    [Header("Timing Settings")]
    [Tooltip("Wait time after destroying 4 cubes.")]
    [SerializeField] private float fourCubeDestructionWaitTime = 1.5f;
    [Tooltip("Wait time after destroying 5+ cubes.")]
    [SerializeField] private float fiveCubeDestructionWaitTime = 2f;
    [Tooltip("Wait time for Special Cube effect.")]
    [SerializeField] private float specialCubeWaitTime = 2f;

    /// <summary>
    /// Checks if the instance already exists, if not, sets this as the instance.
    /// If an instance already exists, destroys this GameObject to avoid duplicates.
    /// Initializes the particle effect dictionaries and the background renderer.
    /// </summary>
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Initialize the ripple block to be able to change its properties safely
        rippleBlock = new MaterialPropertyBlock();
        // Get the background renderer to apply ripple effects
        backgroundRenderer = background.GetComponent<SpriteRenderer>();

        // Initialize the particle effect dictionaries
        InitializeEffectDict();
        InitializeSpecialEffectDict();
    }

    /// <summary>
    /// Maps cube color and match count to particle effects.
    /// </summary>
    private void InitializeEffectDict()
    {
        cubeDestructionEffects = new Dictionary<(CubeColor, int), GameObject>()
        {
            { (CubeColor.Black, 4), fourBlackCubeEffect },
            { (CubeColor.Blue, 4), fourBlueCubeEffect },
            { (CubeColor.Red, 4), fourRedCubeEffect },
            { (CubeColor.Yellow, 4), fourYellowCubeEffect },
            { (CubeColor.Black, 5), fiveBlackCubeEffect },
            { (CubeColor.Blue, 5), fiveBlueCubeEffect },
            { (CubeColor.Red, 5), fiveRedCubeEffect },
            { (CubeColor.Yellow, 5), fiveYellowCubeEffect }
        };
    }

    /// <summary>
    /// Maps cube color to special cube effects.
    /// </summary>
    private void InitializeSpecialEffectDict()
    {
        specialCubeDestructionEffects = new Dictionary<CubeColor, GameObject>()
        {
            { CubeColor.Black, specialBlackCubeEffect },
            { CubeColor.Blue, specialBlueCubeEffect },
            { CubeColor.Red, specialRedCubeEffect },
            { CubeColor.Yellow, specialYellowCubeEffect }
        };
    }

    #region Timers and Flags

    /// <summary>
    /// Returns the destruction delay based on how many cubes were matched.
    /// </summary>
    public float GetColoredCubeDestructionTime(int numberOfCubesDestroyed)
    {
        // Check if the number of cubes destroyed and return the appropriate wait time
        if (numberOfCubesDestroyed == 4)
        {
            return fourCubeDestructionWaitTime;
        }
        else if (numberOfCubesDestroyed >= 5)
        {
            return fiveCubeDestructionWaitTime;
        }
        // If the number of cubes destroyed is not 4 or 5+, return 0 or log a warning
        else
        {
            Debug.LogWarning($"Invalid number of cubes destroyed: {numberOfCubesDestroyed}");
            return 0f;
        }
    }

    /// <summary>
    /// Returns the wait time after playing a special cube effect.
    /// </summary>
    public float GetWaitTimeForSpecialCube()
    {
        return specialCubeWaitTime;
    }

    /// <summary>
    /// Returns true if no visual effect is currently playing.
    /// </summary>
    public bool AreAllEffectsDone()
    {
        return !isEffectActive;
    }

    /// <summary>
    /// Resets the flag that tracks if a five-plus match effect has played.
    /// </summary>
    public void ResetFivePlusEffectFlag()
    {
        hasPlayedFivePlusEffect = false;
    }

    #endregion

    #region Standard Cube Effects

    /// <summary>
    /// Plays the destruction effect for 5-matched cubes with special VFX and audio per color.
    /// </summary>
    /// <param name="cube">The cube used to determine effect and position.</param>
    public ParticleSystem PlayFiveCubesDestructionEffect(Cube cube)
    {
        // Set effect active flag to true to indicate an effect is playing
        isEffectActive = true;

        // Set the number of matches and retrieve the key for the effect
        int numOfMatches = 5;
        var key = (cube.GetColor(), numOfMatches);
        // Get the wait time for the effect based on the number of matches
        float waitTime = GetColoredCubeDestructionTime(numOfMatches);

        // Set the cube color to determine which effect to play
        CubeColor cubeColor = cube.GetColor();

        // Get the cube effect based on color and number of matches
        if (!cubeDestructionEffects.TryGetValue(key, out GameObject effect) || effect == null)
        {
            Debug.LogWarning($"No particle effect assigned for {cubeColor} {numOfMatches} cube destruction.");
            return null;
        }

        // Check if the effect has already played once, if it has, do not play it again
        if (!hasPlayedFivePlusEffect)
        {
            // Once the effect has played, set the flag to true
            hasPlayedFivePlusEffect = true;

            // Play the appropriate effect based on the cube color
            if (cubeColor == CubeColor.Black)
            {
                // Play Black Flames effect for every cube in the group
                PlayFourCubesDestructionEffect(cube);

                foreach (Cube c in cube.GetMatchGroup())
                {
                    PlayFourCubesDestructionEffect(c);
                }
                // Play Reaper Slash effect
                PlayBlackReaperSlash(cube.transform.position + new Vector3(1.5f, 0f, 0f));
                // Shake the screen for more impact
                CameraShaker.Instance.Shake(waitTime, 0.05f);
            }
            else if (cubeColor == CubeColor.Blue)
            {
                // Play Blue Waterfall effect
                PlaySimpleFivePlusEffect(effect, new Vector3(0, 5.5f, 0), Quaternion.identity, waterfallClip);
            }
            else if (cubeColor == CubeColor.Red)
            {
                // Play Red Meteor and Explosion effect
                PlayMeteorAndExplosion(cube.transform.position);
            }
            else if (cubeColor == CubeColor.Yellow)
            {
                // Play Yellow Thunder effect
                PlaySimpleFivePlusEffect(effect, new Vector3(0, 5.5f, 0), Quaternion.Euler(90f, 0f, 0f), thunderClip);
            }
        }

        return null;
    }

    /// <summary>
    /// Plays a simple visual effect at a position and shakes the camera.
    /// </summary>
    /// <param name="effectPrefab">The prefab of the effect.</param>
    /// <param name="position">The position to spawn the effect.</param>
    /// <param name="rotation">The rotation of the transform for the effect.</param>
    /// <param name="sound">The approriate audio clip for the effect.</param>
    /// <param name="shakeIntensity">The instensity of the screen shake, by default it is 0.1.</param>
    private void PlaySimpleFivePlusEffect(GameObject effectPrefab, Vector3 position, Quaternion rotation, AudioClip sound, float shakeIntensity = 0.1f)
    {
        // First, play the sound effect at the specified position
        PlaySFX(sound, position);

        // Spawn the effect from the object pool and set its properties
        GameObject instance = ObjectPoolManager.SpawnObject(effectPrefab, position);
        instance.transform.rotation = rotation;
        instance.transform.localScale = Vector3.one;

        // Set the parent for the effect as ParticlesParent if it exists
        if (particlesParent != null)
            instance.transform.SetParent(particlesParent);

        // Get the ParticleSystem component and play it
        ParticleSystem ps = instance.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Clear(true);
            ps.Play(true);
            StartCoroutine(DespawnAfterParticle(ps));
        }
        // Shake the screen for added effect
        CameraShaker.Instance.Shake(GetColoredCubeDestructionTime(5), shakeIntensity);
    }

    /// <summary>
    /// Plays the destruction effect for 4-matched cubes, scaling visuals based on cube color.
    /// </summary>
    /// <param name="cube">The matched cube.</param>
    public ParticleSystem PlayFourCubesDestructionEffect(Cube cube)
    {
        isEffectActive = true;

        // Set the number of matches, retrieve the key and the wait time for the correct effect
        int numOfMatches = 4;
        var key = (cube.GetColor(), numOfMatches);
        float waitTime = GetColoredCubeDestructionTime(numOfMatches);

        // Reset the scale for the effect
        Vector3 newScale = Vector3.one;

        // Set the scale based on the cube color
        if (cube.GetColor() == CubeColor.Blue)
        {
            newScale = new Vector3(1.2f, 1.2f, 1.2f);
        }
        else if (cube.GetColor() == CubeColor.Yellow)
        {
            newScale = new Vector3(0.3f, 0.3f, 0.3f);
        }
        else
        {
            newScale = new Vector3(0.4f, 0.4f, 0.4f);
        }
        
        // Get the effect for the cube color and the number of matches
        if (!cubeDestructionEffects.TryGetValue(key, out GameObject effect) || effect == null)
        {
            Debug.LogWarning($"No particle effect assigned for {cube.GetColor()} {numOfMatches} cube destruction.");
            return null;
        }

        // Determine position and rotation
        Vector3 spawnPos = cube.transform.position;
        Quaternion rotation = Quaternion.identity;

        // Spawn the effect from the object pool and set its properties
        GameObject instance = ObjectPoolManager.SpawnObject(effect, spawnPos);
        instance.transform.rotation = rotation;
        instance.transform.localScale = newScale;

        // Set the parent for the effect as ParticlesParent if it exists
        if (particlesParent != null)
            instance.transform.SetParent(particlesParent);

        // Get the ParticleSystem component and play it
        ParticleSystem ps = instance.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Clear(true);
            ps.Play(true);
            StartCoroutine(DespawnAfterParticle(ps));
        }
        // Return the ParticleSystem for further control
        return ps;
    }

    #endregion

    #region Particle Lifecycle

    /// <summary>
    /// Despawns a particle effect after it finishes playing or times out.
    /// </summary>
    /// <param name="ps">The Particle System component of the effect.</param>
    private IEnumerator DespawnAfterParticle(ParticleSystem ps)
    {
        // Fallback max duration
        float timeout = 5f;
        float elapsed = 0f;

        while ((ps.isPlaying || ps.particleCount > 0) && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // After the particle system has finished playing, despawn the object
        ObjectPoolManager.DespawnObject(ps.gameObject);

        isEffectActive = false;
    }

    /// <summary>
    /// Despawns a pooled effect object after a fixed duration.
    /// </summary>
    /// <param name="obj">The Game Object to despawn.</param>
    /// <param name="duration">The duration to despawn the object after.</param>
    private IEnumerator DespawnAfterSeconds(GameObject obj, float duration)
    {
        yield return new WaitForSeconds(duration);
        ObjectPoolManager.DespawnObject(obj);

        isEffectActive = false;
    }

    /// <summary>
    /// Despawns an effect after its animation finishes playing.
    /// </summary>
    /// <param name="obj">The Game Object to despawn.</param>
    /// <param name="animator">The animator to get the animation state from.</param>
    private IEnumerator DespawnAfterAnimation(GameObject obj, Animator animator)
    {
        // Wait for the current animation state to finish and then despawn the effect object
        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
        ObjectPoolManager.DespawnObject(obj);
    }

    #endregion

    #region Advanced Effects (Reaper, Meteor, White Cube)

    /// <summary>
    /// Plays the Grim Reaper Slash animation across the board.
    /// </summary>
    /// <param name="targetPosition">The target position to move the Grim Reaper to.</param>
    public void PlayBlackReaperSlash(Vector3 targetPosition)
    {
        isEffectActive = true;

        // Set the starting position for the Reaper as outside of the screen
        Vector3 startPos = new Vector3(4.5f, targetPosition.y, 0f);
        // Spawn the Reaper effect from the object pool
        GameObject reaper = ObjectPoolManager.SpawnObject(fiveBlackCubeEffect, startPos);
        // Set its parent to particlesParent if it exists
        if (particlesParent != null)
            reaper.transform.SetParent(particlesParent);

        // Get the Animator component from the Reaper
        Animator animator = reaper.GetComponent<Animator>();

        // Start the coroutine to move the Reaper and play the animation
        StartCoroutine(MoveAndPlayReaper(reaper, targetPosition, animator));
    }

    /// <summary>
    /// Moves the Reaper to a target position and plays animation and particles.
    /// </summary>
    /// <param name="reaper">The Grim Reaper Game Object.</param>
    /// <param name="targetPos">The target position to move the Grim Reaper to.</param>
    /// <param name="animator">The animator to play the slash animation.</param>
    private IEnumerator MoveAndPlayReaper(GameObject reaper, Vector3 targetPos, Animator animator)
    {
        // Set the move duration and elapsed time
        float duration = 1f;
        float elapsed = 0f;
        // Get the starting position of the Reaper
        Vector3 start = reaper.transform.position;
        // Play Evil Laugh sound at the center
        PlaySFX(evilLaughClip, new Vector2(0, 0));

        // Move the Grim Reaper from start to target position
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            reaper.transform.position = Vector3.Lerp(start, targetPos, t);
            yield return null;
        }
        // Ensure the Reaper reaches the target position
        reaper.transform.position = targetPos;

        // Activate the Souls Leaving effect
        Transform soulsLeaving = reaper.transform.Find("SoulsLeaving");
        Vector3 soulsPosition = new Vector3(targetPos.x - 1.5f, targetPos.y, targetPos.z);
        soulsLeaving.transform.position = soulsPosition;
        soulsLeaving.gameObject.SetActive(true);

        // Play the Slash animation and despawn after it finishes
        if (animator != null)
        {
            animator.Play("ReaperSlash", -1, 0f);
            StartCoroutine(DespawnAfterAnimation(reaper, animator));
        }

        // Get the ParticleSystem from the SoulsLeaving effect
        ParticleSystem ps = soulsLeaving.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            yield return new WaitUntil(() => !ps.isPlaying && ps.particleCount == 0);
        }
        else
        {
            // Fallback if no ParticleSystem is found
            yield return new WaitForSeconds(0.5f);
        }
        // Deactivate the SoulsLeaving effect after it's done
        soulsLeaving.gameObject.SetActive(false);

        isEffectActive = false;
    }

    /// <summary>
    /// Plays meteor drop animation followed by explosion.
    /// </summary>
    /// <param name="targetPos">The target position to move the Meteor to.</param>
    private void PlayMeteorAndExplosion(Vector3 targetPos)
    {
        isEffectActive = true;
        // Set the starting position for the Meteor to be above the screen and around the same column as the target position
        Vector3 startPos = new Vector3(targetPos.x, 5f, 0f);
        // Randomize the angle of the Meteor for a more dynamic effect
        float randomZRotation = Random.Range(-20f, 20f);

        // Spawn the Meteor and Explosion effect from the object pool
        GameObject meteorAndExplosion = ObjectPoolManager.SpawnObject(fiveRedCubeEffect, startPos);
        // Set the parent for the effect as ParticlesParent if it exists
        if (particlesParent != null) meteorAndExplosion.transform.SetParent(particlesParent);

        // Find the Meteor and Explosion transforms within the root object
        Transform meteor = meteorAndExplosion.transform.Find("Meteor");
        Transform explosion = meteorAndExplosion.transform.Find("Explosion");
        // Change the angle of the Meteor to add some rotation
        meteor.transform.rotation = Quaternion.Euler(0f, 0f, randomZRotation);

        // Play the Meteor and Explosion effect
        StartCoroutine(MoveMeteorAndExplode(meteorAndExplosion, meteor, explosion, targetPos));
    }

    /// <summary>
    /// Handles meteor's movement, impact, explosion effect, and cleanup.
    /// </summary>
    /// <param name="root">The root Game Object that holds the Meteor and the Explosion Particle System.</param>
    /// <param name="meteor">The transform of the Meteor.</param>
    /// <param name="explosion">The transform of the Explosion.</param>
    /// <param name="targetPos">The target position to move the Meteor to.</param>
    private IEnumerator MoveMeteorAndExplode(GameObject root, Transform meteor, Transform explosion, Vector3 targetPos)
    {
        // Set the duration of the Meteor drop and the make the target position
        // is slightly above the target cube to create a realistic hit effect
        float duration = 0.6f;
        targetPos.y += 0.25f;
        // Set the starting position of the Meteor
        Vector3 start = root.transform.position;
        float elapsed = 0f;

        // Play Meteor sound
        PlaySFX(meteorClip, targetPos);

        // Move the Meteor from the starting position to the target position
        while (elapsed < duration)
        {
            root.transform.position = Vector3.Lerp(start, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        // Ensure the Meteor reaches the target position
        root.transform.position = targetPos;

        // Activate explosion by disabling the Meteor and enabling the Explosion
        meteor.gameObject.SetActive(false);
        explosion.gameObject.SetActive(true);

        // Play Explosion sound
        PlaySFX(explosionClip, targetPos);

        // Shake the screen for impact
        CameraShaker.Instance.Shake(1.4f, 0.3f);

        // Wait for explosion particle to finish
        ParticleSystem ps = explosion.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            yield return new WaitUntil(() => !ps.isPlaying && ps.particleCount == 0);
        }
        else
        {
            yield return new WaitForSeconds(1.4f);
        }

        // Reset children for reuse of the effect
        meteor.gameObject.SetActive(true);
        explosion.gameObject.SetActive(false);

        // Despawn root Game Object into the object pool
        ObjectPoolManager.DespawnObject(root);

        isEffectActive = false;
    }

    /// <summary>
    /// Triggers White Cube destruction chromatic ripple effect.
    /// </summary>
    /// <param name="position">The position to play the White Cube destruction effect.</param>
    /// <param name="waitDuration">The duration to wait for the effect to complete.</param>
    public IEnumerator PlayWhiteCubeDestructionEffect(Vector3 position, float waitDuration)
    {
        isEffectActive = true;

        // Enable ripple safely
        backgroundRenderer.GetPropertyBlock(rippleBlock);
        rippleBlock.SetFloat("_RippleCount", 1f);
        rippleBlock.SetVector("_RippleCenter", position);
        rippleBlock.SetFloat("_EnableRipple", 1f);
        rippleBlock.SetFloat("_RippleMode", 0f); // 0 = chromatic colors effect
        backgroundRenderer.SetPropertyBlock(rippleBlock);

        // Play Ripple sound
        PlaySFX(rippleClip, position);
        yield return new WaitForSeconds(waitDuration);

        // Fade out ripple safely
        backgroundRenderer.GetPropertyBlock(rippleBlock);
        rippleBlock.SetFloat("_RippleCount", 0f);
        rippleBlock.SetFloat("_EnableRipple", 0f); // disable ripple
        backgroundRenderer.SetPropertyBlock(rippleBlock);

        // Set effect active flag to false
        isEffectActive = false;
    }

    /// <summary>
    /// Triggers matched white cube ripple effect (Black and White hypnotic).
    /// </summary>
    /// <param name="position">The position to play the two White Cubes destruction effect.</param>
    /// <param name="waitDuration">The duration to wait for the effect to complete.</param>
    public IEnumerator PlayMatchedWhiteCubesDestructionEffect(Vector3 position, float waitDuration)
    {
        isEffectActive = true;

        // Enable black and white ripple safely
        backgroundRenderer.GetPropertyBlock(rippleBlock);
        rippleBlock.SetFloat("_RippleCount", 1f);
        rippleBlock.SetVector("_RippleCenter", position);
        rippleBlock.SetFloat("_EnableRipple", 1f);
        rippleBlock.SetFloat("_RippleMode", 1f); // 1 = black-and-white hypnotic
        backgroundRenderer.SetPropertyBlock(rippleBlock);

        // Play Ripple sound
        PlaySFX(rippleClip, position);
        yield return new WaitForSeconds(waitDuration);

        // Fade out ripple effect safely
        backgroundRenderer.GetPropertyBlock(rippleBlock);
        rippleBlock.SetFloat("_RippleCount", 0f);
        rippleBlock.SetFloat("_EnableRipple", 0f); // hide ripple
        rippleBlock.SetFloat("_RippleMode", 0f); // Reset to chromatic
        backgroundRenderer.SetPropertyBlock(rippleBlock);

        isEffectActive = false;
    }

    /// <summary>
    /// Plays the final white glow VFX used for full board clear.
    /// </summary>
    /// <param name="position">The position to play the White Cube destruction effect.</param>
    /// <param name="waitDuration">The duration to wait for the effect to complete.</param>
    public IEnumerator PlayUltimateWhiteGlow(Vector3 position, float waitDuration)
    {
        isEffectActive = true;

        // Spawn the ultimate white glow effect at the specified position
        GameObject instance = ObjectPoolManager.SpawnObject(ultimateWhiteGlowVFX, position);
        instance.transform.localScale = Vector3.one;
        instance.transform.rotation = Quaternion.identity;

        // Set the parent for the effect as ParticlesParent if it exists
        if (particlesParent != null)
            instance.transform.SetParent(particlesParent);

        // Get the ParticleSystem component and play it
        ParticleSystem ps = instance.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Clear(true);
            ps.Play(true);

            // Manually stop after the wait duration
            yield return new WaitForSeconds(waitDuration);

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        // Despawn the effect after it finishes playing
        ObjectPoolManager.DespawnObject(instance);
        isEffectActive = false;
    }

    #endregion

    #region Special Cube and Global FX

    /// <summary>
    /// Plays a special cube effect, optionally with LineRenderer targeting.
    /// </summary>
    /// <param name="color">The color of the Special Cube.</param>
    /// <param name="from">The start position of the effect.</param>
    /// <param name="to">The position that the Yellow Zap reaches.</param>
    public void PlaySpecialCubeEffect(CubeColor color, Vector3 from, Vector3 to)
    {
        isEffectActive = true;

        // Get the Special Cube effect based on color
        if (specialCubeDestructionEffects.TryGetValue(color, out GameObject effect))
        {
            // Spawn the effect from the object pool
            GameObject instance = ObjectPoolManager.SpawnObject(effect, from);
            // Set the parent for the effect as ParticlesParent if it exists
            if (particlesParent != null)
                instance.transform.SetParent(particlesParent);

            // If the color is Yellow, set up the LineRenderer
            if (color == CubeColor.Yellow)
            {
                // Get the LineRenderer component and set its positions
                LineRenderer lr = instance.GetComponent<LineRenderer>();
                if (lr != null)
                {
                    lr.positionCount = 2;
                    lr.SetPosition(0, from);
                    lr.SetPosition(1, to);
                }
            }

            // Play the ParticleSystem component and despawn the effect after the appropriate wait time
            StartCoroutine(DespawnAfterSeconds(instance, specialCubeWaitTime));
        }

        isEffectActive = false;
    }

    /// <summary>
    /// Fades in, holds, and fades out a white flash overlay.
    /// </summary>
    /// <param name="fadeIn">The duration to fade in the white flash.</param>
    /// <param name="hold">The duration to hold the white flash on the screen.</param>
    /// <param name="fadeOut">The duration to fade out the white flash.</param>
    public IEnumerator PlayWhiteScreenGlow(float fadeIn = 0.2f, float hold = 0.3f, float fadeOut = 0.8f)
    {
        isEffectActive = true;

        // Get the white screen glow image component
        Image whiteScreenGlowImage = whiteScreenGlow.GetComponent<Image>();
        if (whiteScreenGlowImage == null)
        {
            Debug.LogError("White screen glow image component not found.");
            yield break;
        }
        // Set it active to start the effect
        whiteScreenGlowImage.gameObject.SetActive(true);
        UnityEngine.Color c = whiteScreenGlowImage.color;

        // Fade in the white screen glow
        float t = 0f;
        while (t < fadeIn)
        {
            t += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0, 1, t / fadeIn);
            whiteScreenGlowImage.color = new UnityEngine.Color(c.r, c.g, c.b, alpha);
            yield return null;
        }

        // Hold full white on the screen
        yield return new WaitForSecondsRealtime(hold);

        // Fade out the white screen glow
        t = 0f;
        while (t < fadeOut)
        {
            t += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1, 0, t / fadeOut);
            whiteScreenGlowImage.color = new UnityEngine.Color(c.r, c.g, c.b, alpha);
            yield return null;
        }

        // Deactivate the white screen glow image after fading out
        whiteScreenGlowImage.gameObject.SetActive(false);

        isEffectActive = false;
    }

    /// <summary>
    /// Temporarily applies a low-pass filter to the music.
    /// </summary>
    /// <param name="duration">The duration to muffle the music for.</param>
    public IEnumerator TemporarilyMuffleMusic(float duration = 1f)
    {
        // Lower the music frequency to create a muffled effect
        if (BackgroundMusicPlayer.Instance != null)
            BackgroundMusicPlayer.Instance.ApplyLowPass(800f);

        yield return new WaitForSecondsRealtime(duration);

        // Restore the music frequency to normal after the duration
        if (BackgroundMusicPlayer.Instance != null)
            BackgroundMusicPlayer.Instance.ApplyLowPass(22000f);

    }

    /// <summary>
    /// Plays a sound effect at a given world position.
    /// </summary>
    /// <param name="clip">The audio clip of the Sound FX of the effect to play.</param>
    /// <param name="position">The position to play the effect.</param>
    private void PlaySFX(AudioClip clip, Vector3 position)
    {
        if (BackgroundMusicPlayer.SoundFXEnabled && clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, position);
        }
    }

    #endregion
}
