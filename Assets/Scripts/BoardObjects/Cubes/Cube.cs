using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Cube : BoardObject
{
    // Boolean to check if the cube has more than 2 matches
    public bool HasMatch { get; private set; } = false;

    // Boolean to check if the cube is special (i.e. spawned after 4 cube destruction)
    [SerializeField] private bool isSpecial = false;

    // Property to check if the cube is special
    public bool IsSpecial => isSpecial;

    // Color of the cube
    [SerializeField] private CubeColor color;
    // List of cubes that are connected and have the same color as this cube
    private List<Cube> matches = new List<Cube>();
    // Set the match type
    [SerializeField] private MatchType matchType = MatchType.None;

    // Reference to the SpriteRenderer component for this cube
    private SpriteRenderer spriteRenderer;

    /// <summary>
    /// Returns the color of this cube.
    /// </summary>
    public CubeColor GetColor()
    {
        return color;
    }

    /// <summary>
    /// Adds the given amount of matched cubes to the list of matches of this cube.
    /// </summary>
    /// <param name="matchedCubes">The list of matched cubes to add the matched cube group of this cube.</param>
    public void AddMatchedCubesToList(List<Cube> matchedCubes)
    {
        matches.AddRange(matchedCubes);
    }

    /// <summary>
    /// Clears the list of matched cubes of this cube.
    /// </summary>
    public void ClearMatches()
    {
        matches.Clear();    
    }

    /// <summary>
    /// Changes the match type of this cube to the given match type.
    /// </summary>
    /// <param name="matchType">The new match type of this cube.</param>
    public void SetMatchType(MatchType matchType)
    {
        this.matchType = matchType;
    }

    /// <summary>
    /// Returns the match type of this cube.
    /// </summary>
    public MatchType GetMatchType()
    {
        return matchType;
    }

    /// <summary>
    /// Sets the match type to the given match type if this cube has a no match type.
    /// If this cube already has a match type, it will set the match type to MatchType.Both if the new type is different.
    /// </summary>
    /// <param name="matchType">The new match type of this cube.</param>
    public void AddMatchType(MatchType newType)
    {
        // Check the match type of this cube, if it is None, set it to the new type
        if (matchType == MatchType.None)
            matchType = newType;
        // If the match type is already set, check if the new type is different
        else if (matchType != newType)
            // If it is different, set the match type to Both
            matchType = MatchType.Both;
    }

    /// <summary>
    /// Sets if this cube has a match or not (i.e. cube is in a matched group)
    /// </summary>
    /// <param name="value">The boolean of if it has a match or not.</param>
    public void SetMatch(bool value)
    {
        HasMatch = value;
    }

    /// <summary>
    /// Returns the number of cubes in the match group, including this cube.
    /// </summary>
    public int GetMatchGroupSize()
    {
        // Return the number of the matched cubes and +1 for this cube for the whole group size
        return matches.Count + 1;
    }

    /// <summary>
    /// Returns the list of matched cubes of this cube.
    /// </summary>
    public List<Cube> GetMatchGroup()
    {
        return new List<Cube>(matches); // return a copy to preserve encapsulation
    }

    /// <summary>
    /// Registers the given horizontal matches for this cube and adds the matched cubes to the list of matches.
    /// </summary>
    /// <param name="matchedCubes">The horizontally matched cubes for this cube.</param>
    public void RegisterHorizontalMatches(List<Cube> matchedCubes)
    {
        // Set the HasMatch boolean to true and add the match type
        HasMatch = true;
        AddMatchType(MatchType.Horizontal);
        // Add the matched cubes to the list of matches
        AddMatchedCubesToList(matchedCubes);

        // Set the match state of the matched cubes to true and add the match type to each matched cube
        foreach (Cube cube in matchedCubes)
        {
            cube.SetMatch(true);
            cube.AddMatchType(MatchType.Horizontal);
        }
    }

    /// <summary>
    /// Registers the given vertical matches for this cube and adds the matched cubes to the list of matches.
    /// </summary>
    /// <param name="matchedCubes">The vertically matched cubes for this cube.</param>
    public void RegisterVerticalMatches(List<Cube> matchedCubes)
    {
        // Do the same as RegisterHorizontalMatches, but for vertical matches
        HasMatch = true;
        AddMatchType(MatchType.Vertical);
        AddMatchedCubesToList(matchedCubes);

        foreach(Cube cube in matchedCubes)
        {
            cube.SetMatch(true);
            cube.AddMatchType(MatchType.Vertical);  
        }
    }

    /// <summary>
    /// Resets the match state of this cube, clearing the matches and setting the match type to None.
    /// </summary>
    public void ResetMatchState()
    {
        HasMatch = false;
        this.ClearMatches();
        this.SetMatchType(MatchType.None);
    }

    /// <summary>
    /// Converts this cube to a WhiteCube and notifies the target, playing a pop or shake animation.
    /// </summary>
    /// <param name="pop">The boolean to determine if the effect is a pop effect or a shake effect, the default is pop effect.</param>
    public WhiteCube ConvertToWhiteAndNotifyTarget(bool pop = true)
    {
        // If the cube is already white, return null to avoid unnecessary conversion
        if (GetColor() == CubeColor.White) return null;

        // Get the current indices of this cube
        int x = GetX();
        int y = GetY();

        // Safely despawn this cube
        ObjectUtils.SafeDespawn(this);

        // Set the spawn position based on the current indices
        Vector2 spawnPos = GameBoard.Instance.FindPositionsGivenIndices(x, y);
        // Spawn a new WhiteCube at the specified position using the ObjectPoolManager
        GameObject whiteCubeGO = ObjectPoolManager.SpawnObjectByKey("WhiteCube", spawnPos);
        // Get the WhiteCube component from the spawned GameObject
        WhiteCube whiteCube = whiteCubeGO.GetComponent<WhiteCube>();

        // Ensure the parent of the cube, transform and index are set correctly
        whiteCube.transform.SetParent(GameBoard.Instance.transform);
        whiteCube.transform.localScale = Vector3.one;
        whiteCube.transform.localPosition = spawnPos;
        whiteCube.SetIndices(x, y);
        // Clear the matches and reset the match state to be sure of the new White Cube
        whiteCube.ResetMatchState();

        // Update the board tile with the new WhiteCube
        GameBoard.Instance.GetBoardTileAt(x, y).SetObjectInside(whiteCubeGO);

        // Check if the whiteCubeGO is active and play the appropriate animation
        if (whiteCube != null && whiteCubeGO.activeInHierarchy)
        {
            if (pop)
                whiteCube.StartCoroutine(whiteCube.PlayPopAnimation());
            else
                whiteCube.StartCoroutine(whiteCube.Shake());
        }
        // Return the newly created WhiteCube
        return whiteCube;
    }

    /// <summary>
    /// Plays a pop animation on this cube, scaling it up and down to create a popping effect.
    /// </summary>
    /// <param name="popScale">The new scale of the cube to give the pop effect.</param>
    /// <param name="duration">The duration of the pop effect.</param>
    public IEnumerator PlayPopAnimation(float popScale = 1.3f, float duration = 0.2f)
    {
        // Get the transform of this cube
        Transform cubeTransform = transform;
        // Store the original scale and calculate the target scale based on the popScale
        Vector3 originalScale = cubeTransform.localScale;
        Vector3 targetScale = originalScale * popScale;

        // Scale the cube up to the target scale over the duration
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            cubeTransform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        // Ensure the cube is scaled to the target scale at the end
        cubeTransform.localScale = targetScale;

        // Scaling it back down
        elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            cubeTransform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        // Ensure the cube is back to its original scale
        cubeTransform.localScale = originalScale;
    }

    /// <summary>
    /// Moves this cube to the specified center position and despawns it after the duration.
    /// </summary>
    /// <param name="center">The center of the screen position.</param>
    /// <param name="duration">The duration of the move.</param>
    public IEnumerator MoveToAndDespawn(Vector3 center, float duration = 0.5f)
    {
        // Get the starting position of this cube
        Vector3 start = transform.localPosition;
        float elapsed = 0f;
        // Move the cube to the center position over the specified duration
        while (elapsed < duration)
        {
            transform.localPosition = Vector3.Lerp(start, center, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        // Ensure the cube is at the center position at the end
        transform.localPosition = center;
        // Then despawn the cube safely, while not decreasing the target count for this cube
        ObjectUtils.SafeDespawn(this, false);
    }
}

public enum CubeColor
{
    Red,
    Yellow,
    Blue,
    Black,
    White
}

public enum MatchType
{
    Horizontal,
    Vertical,
    Both,
    None
}