using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for all board objects. Stores grid indices and handles movement with smooth movement.
/// </summary>
public class BoardObject : MonoBehaviour
{
    // Flag for if the board object is moving. Set to false by default
    public bool IsMoving { get; private set; } = false;

    // Coroutine to handle the movement of the cube
    private Coroutine moveRoutine;

    [Header("Grid Position")]
    [Tooltip("The X index of the object on the board.")]
    [SerializeField] private int x;
    [Tooltip("The Y index of the object on the board.")]
    [SerializeField] private int y;

    [Header("Movement Settings")]
    [Tooltip("Duration it takes for the object to move to the target position.")]
    [SerializeField] private float moveDuration = 0.1f;

    [Tooltip("Delay after movement finishes before IsMoving is reset.")]
    [SerializeField] private float postMoveDelay = 0.1f;

    [Tooltip("Movement easing curve for smooth movement animation.")]
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    /// <summary>
    /// Sets the object's current grid indices.
    /// </summary>
    /// <param name="x">The X index of the board object.</param>
    /// <param name="y">The Y index of the board object.</param>
    public void SetIndices(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    /// <summary>
    /// Returns the X index of the object.
    /// </summary>
    public int GetX()
    {
        return x;
    }

    /// <summary>
    /// Returns the Y index of the object.
    /// </summary>
    public int GetY()
    {
        return y;
    }

    /// <summary>
    /// Animates the object moving to the target position using a smooth movement curve.
    /// </summary>
    /// <param name="targetPos">The position to move the board object to.</param>
    public void MoveToPosition(Vector2 targetPos)
    {
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"[MoveToPosition] Tried to move inactive cube: {name}");
            return;
        }

        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(AnimateMove(transform.localPosition, targetPos));
    }

    /// <summary>
    /// Coroutine that handles board object movement.
    /// </summary>
    /// <param name="startPos">The start position of the board object.</param>
    /// <param name="targetPos">The target position to move the board object to.</param>
    private IEnumerator AnimateMove(Vector2 startPos, Vector2 targetPos)
    {
        // Set the board object as moving at the start of the coroutine
        IsMoving = true;

        float elapsed = 0f;
        // Move the object within the move duration
        while (elapsed < moveDuration)
        {
            float t = elapsed / moveDuration;
            float easedT = movementCurve.Evaluate(t);
            transform.localPosition = Vector2.Lerp(startPos, targetPos, easedT);
            elapsed += Time.deltaTime;
            yield return null;
        }
        // Ensure the board object reaches its target position
        transform.localPosition = targetPos;

        // Add delay after movement before marking it as finished
        yield return new WaitForSeconds(postMoveDelay);

        IsMoving = false;
    }

    /// <summary>
    /// Combines setting indices and triggering the object's movement.
    /// </summary>
    /// <param name="x">The new X index to drop the board object to.</param>
    /// <param name="y">The new Y index to drop the board object to.</param>
    /// <param name="targetPos">The target position to move the board object to.</param>
    public void MoveToPositionAndUpdateIndices(int x, int y, Vector2 targetPos)
    {
        // Update the indices of the board object and move it to the target position
        SetIndices(x, y);
        MoveToPosition(targetPos);
    }

    /// <summary>
    /// Plays a shake animation on this Board Object.
    /// </summary>
    /// <param name="duration">The duration of the shake effect.</param>
    /// <param name="strengthMin">Minimum shake strength.</param>
    /// <param name="strengthMax">Maximum shake strength.</param>
    public IEnumerator Shake(float duration = 0.2f, float strengthMin = 0.05f, float strengthMax = 0.2f)
    {
        // Get the original position of the Board Object
        Vector3 originalPos = transform.localPosition;

        // Set the initial elapsed time and the strenght of the shake
        float elapsed = 0f;
        float strength = Random.Range(strengthMin, strengthMax);

        // Shake the Board Object by moving it's position over the duration
        while (elapsed < duration)
        {
            float offsetX = Random.Range(-strength, strength);
            float offsetY = Random.Range(-strength, strength);
            transform.localPosition = originalPos + new Vector3(offsetX, offsetY, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }
        // Ensure the final position of the object is its initial position
        transform.localPosition = originalPos;
    }
}
