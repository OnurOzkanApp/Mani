using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class WhiteCube : Cube
{
    /// <summary>
    /// Finds all the cubes on the current Game Board of the given cube color.
    /// Then converts them to white cubes, and despawns them after a delay.
    /// Damages all the adjacent obstacles and increments the combo count.
    /// </summary>
    /// <param name="colorToDestroy">The color of the cube to destroy all the cubes of.</param>
    public IEnumerator ActivateColorClear(CubeColor colorToDestroy)
    {
        // Wait for the particle effect to play
        float wait = 2.5f;
        // Play the particle effect for white cube destruction at the cube's position
        StartCoroutine(ParticleManager.Instance.PlayWhiteCubeDestructionEffect(transform.position, wait));

        // Find all cubes of the specified color on the board
        List<Cube> targets = FindObjectsOfType<Cube>().Where(c => c.GetColor() == colorToDestroy).ToList();

        // Create a new list to hold the white cubes to despawn
        List<Cube> whiteCubesToDespawn = new();

        // Go through each target cube
        foreach (Cube cube in targets)
        {
            // Convert the cube to a white cube and notify the target count
            WhiteCube newWhite = cube.ConvertToWhiteAndNotifyTarget();
            // If the conversion was successful, add the new white cube to the despawn list
            if (newWhite != null)
                whiteCubesToDespawn.Add(newWhite);
        }

        // Wait for the particle effect to finish playing
        yield return new WaitForSeconds(wait);

        // Add the original white cube to the targets list
        targets.Add(this);

        // Damage all adjacent obstacles next to the cubes that were same color as the given color
        GameBoard.Instance.DamageAdjacentObstacles(targets);

        // Go through each white cube and despawn it
        foreach (Cube white in whiteCubesToDespawn)
        {
            ObjectUtils.SafeDespawn(white, false);
        }
        // Despawn the original white cube
        ObjectUtils.SafeDespawn(this);

        // Increment the combo count and show the combo text at the white cube's position
        ComboManager.Instance.IncrementCombo();
        ComboManager.Instance.ShowComboTextAt(this.transform);

        // Wait until all particle effects are done playing and all cubes have finished moving
        yield return new WaitUntil(() => ParticleManager.Instance.AreAllEffectsDone());
        yield return new WaitUntil(() => GameBoard.Instance.AllCubesFinishedMoving());

    }

    /// <summary>
    /// Goes through the rows and columns of this White Cube and the given White Cube.
    /// Converts and destroys all the cubes in those rows and columns while damaging adjacent obstacles,
    /// and increments the combo count.
    /// </summary>
    /// <param name="otherWhiteCube">The other White Cube that was swapped with this White Cube.</param>
    public IEnumerator DoubleWhiteCubeDestruction(Cube otherWhiteCube)
    {
        // Create a HashSet to keep track of the white cubes that were converted
        HashSet<Cube> whiteConvertedCubes = new();

        // Get the indices of both white cubes
        int x1 = this.GetX();
        int y1 = this.GetY();
        int x2 = otherWhiteCube.GetX();
        int y2 = otherWhiteCube.GetY();

        // Get the width and height of the current level
        int width = LevelManager.Instance.GetGridWidth();
        int height = LevelManager.Instance.GetGridHeight();

        // Only loop through each row/col once by checking if the cubes are in the same row
        if (y1 == y2)
        {
            // If they are in the same row, go through the row and process only once
            for (int i = 0; i < width; i++)
            {
                // Skip the columns where the white cubes are located
                if (i == x1 || i == x2)
                {
                    continue;
                }
                // Convert the cubes in the row to white cubes
                yield return StartCoroutine(GameBoard.Instance.TryConvertToWhiteCube(i, y1, whiteConvertedCubes));
            }
        }
        // If they are not in the same row, process both rows
        else
        {
            // Process both rows
            for (int i = 0; i < width; i++)
            {
                if (i == x1 || i == x2)
                {
                    continue;
                }
                // Convert the cubes in both of the rows to white cubes
                yield return StartCoroutine(GameBoard.Instance.TryConvertToWhiteCube(i, y1, whiteConvertedCubes));
                yield return StartCoroutine(GameBoard.Instance.TryConvertToWhiteCube(i, y2, whiteConvertedCubes));
            }
        }

        // If the cubes are in the same column, process only once
        if (x1 == x2)
        {
            // Same column: process once
            for (int j = 0; j < height; j++)
            {
                if (j == y1 || j == y2)
                {
                    continue;
                }
                yield return StartCoroutine(GameBoard.Instance.TryConvertToWhiteCube(x1, j, whiteConvertedCubes));
            }
        }
        // If they are not in the same column, process both columns
        else
        {
            for (int j = 0; j < height; j++)
            {
                if (j == y1 || j == y2)
                {
                    continue;
                }
                yield return StartCoroutine(GameBoard.Instance.TryConvertToWhiteCube(x1, j, whiteConvertedCubes));
                yield return StartCoroutine(GameBoard.Instance.TryConvertToWhiteCube(x2, j, whiteConvertedCubes));
            }
        }

        // Remove the original white cubes from the converted cubes list
        whiteConvertedCubes.Remove(this);
        whiteConvertedCubes.Remove(otherWhiteCube);

        // Set the wait duration for the particle effect
        float waitDuration = 2.5f;

        // Play the particle effect for both white cubes
        GameBoard.Instance.StartCoroutine(ParticleManager.Instance.PlayWhiteCubeDestructionEffect(this.transform.position, waitDuration));
        GameBoard.Instance.StartCoroutine(ParticleManager.Instance.PlayWhiteCubeDestructionEffect(otherWhiteCube.transform.position, waitDuration));

        // Wait for the particle effect to finish playing
        yield return new WaitForSeconds(waitDuration);

        // Despawn all the converted white cubes
        foreach (Cube cube in whiteConvertedCubes)
        {
            if (cube != null && cube.gameObject.activeSelf)
                GameBoard.Instance.DespawnCube(cube, false);
        }

        // Despawn the original white cubes
        GameBoard.Instance.DespawnCube(this);
        GameBoard.Instance.DespawnCube((WhiteCube)otherWhiteCube);

        // Create a list of cubes to trigger the damage effect
        List<Cube> cubesToTrigger = whiteConvertedCubes.ToList();
        // Add the original white cubes to the list
        cubesToTrigger.Add(this);
        cubesToTrigger.Add(otherWhiteCube);
        // Damage all adjacent obstacles next to the cubes that were converted
        GameBoard.Instance.DamageAdjacentObstacles(cubesToTrigger);

        // Increment the combo count and show the combo text at this white cube's position
        ComboManager.Instance.IncrementCombo();
        ComboManager.Instance.ShowComboTextAt(this.transform);

        // Wait until all particle effects are done playing and all cubes have finished moving
        yield return new WaitUntil(() => ParticleManager.Instance.AreAllEffectsDone());
        yield return new WaitUntil(() => GameBoard.Instance.AllCubesFinishedMoving());
    }

    /// <summary>
    /// Destroys the entire board by converting all cubes to white cubes, shaking them, and despawning them.
    /// </summary>
    public IEnumerator DestroyTheBoard()
    {
        // Create a list to hold the cubes to despawn and the count to decrease the target count for that cube color
        List<(Cube cube, bool decreaseTargetCount)> cubesToDespawn = new();

        // Get the current width and height of the grid
        int currentWidth = LevelManager.Instance.GetGridWidth();
        int currentHeight = LevelManager.Instance.GetGridHeight();

        // Play the particle effect for matched white cubes destruction at the center of the board
        GameBoard.Instance.StartCoroutine(ParticleManager.Instance.PlayMatchedWhiteCubesDestructionEffect(Vector3.zero, 3f));

        // Loop through each tile in the grid
        for (int x = 0; x < currentWidth; x++)
        {
            for (int y = 0; y < currentHeight; y++)
            {
                // Get the board tile at the current indices
                BoardTile tile = GameBoard.Instance.GetBoardTileAt(x, y);
                // If the tile is not empty, get the object inside
                if (!tile.GetEmpty())
                {
                    GameObject obj = tile.GetObjectInside();

                    // If the object is a Cube, convert it to a White Cube and shake it
                    if (obj != null && obj.TryGetComponent<Cube>(out Cube cube))
                    {
                        // Check if the cube is already white
                        bool isWhite = cube.GetColor() == CubeColor.White;

                        // If the cube is not white, convert it to a white cube and notify the target count
                        if (!isWhite)
                        {
                            WhiteCube newWhite = cube.ConvertToWhiteAndNotifyTarget(false);
                            // If the conversion was successful, add the new white cube to the despawn list
                            if (newWhite != null)
                                cubesToDespawn.Add((newWhite, false));
                        }
                        else
                        {
                            // If the cube is already white, just shake it and add it to the despawn list
                            cube.StartCoroutine(cube.Shake());
                            cubesToDespawn.Add((cube, true));
                        }
                        // Reset the match state of the cube
                        cube.ResetMatchState();
                    }
                    // If the object is an Obstacle, do the max damage (i.e. despawn it) and update the target count
                    else if (obj != null && obj.TryGetComponent<Obstacle>(out Obstacle obstacle))
                    {
                        ObjectUtils.SafeDespawn(obstacle.gameObject);
                    }

                }
            }
        }

        // Wait until all the cubes are popped and ready to be destroyed
        yield return new WaitForSeconds(2.7f);

        // Play the white screen glow effect and muffle the music for impact
        GameBoard.Instance.StartCoroutine(ParticleManager.Instance.PlayWhiteScreenGlow());
        GameBoard.Instance.StartCoroutine(ParticleManager.Instance.TemporarilyMuffleMusic());

        // Go through each cube in the despawn list and despawn it while decreasing the target count by the specified amount
        foreach (var (cube, shouldDecreaseTarget) in cubesToDespawn)
        {
            ObjectUtils.SafeDespawn(cube, shouldDecreaseTarget);
        }

        // Increment the combo count and show the combo text at this white cube's position
        ComboManager.Instance.IncrementCombo();
        ComboManager.Instance.ShowComboTextAt(this.transform);

        // Wait until all particle effects are done playing and all cubes have finished moving
        yield return new WaitUntil(() => ParticleManager.Instance.AreAllEffectsDone());
        yield return new WaitUntil(() => GameBoard.Instance.AllCubesFinishedMoving());

    }

    /// <summary>
    /// Triggers the final bonus effect of the White Cube, which pulls all cubes of random colors
    /// from the board and destroys them depending on the given number of White Cubes left after
    /// all cascades are done.
    /// </summary>
    /// <param name="numOfWhiteCubes">The number of White Cubes remaining on the board after all cascades are complete.</param>
    public IEnumerator TriggerFinalBonusEffect(int numOfWhiteCubes)
    {
        // Calculate the number of colors to destroy based on the number of White Cubes left
        // It there are 1-2 White Cubes left, destroy 1 color; if there are 3-4, destroy 2 colors;
        // if there are 5-6, destroy 3 colors; and if there are 7 or more, destroy 4 colors.
        int numOfColorsToDestroy = Mathf.Clamp((numOfWhiteCubes + 1) / 2, 1, 4);

        // Set the visual delay for the particle effects
        float visualDelay = 2.5f;

        // Get the SortingGroup component of this White Cube and increase the sorting order to ensure the cube is rendered on top
        SortingGroup group = GetComponent<SortingGroup>();
        int originalOrder = group.sortingOrder;
        group.sortingOrder += 1;

        // Define all possible cube colors
        CubeColor[] allColors = { CubeColor.Red, CubeColor.Blue, CubeColor.Yellow, CubeColor.Black };

        // Select random colors to destroy depending on the number of colors to destroy
        List<CubeColor> shuffled = allColors.OrderBy(_ => Random.value).ToList();
        List<CubeColor> selectedColors = shuffled.Take(numOfColorsToDestroy).ToList();

        // Play the particle effect for the ultimate white glow and riplle effect at the cube's position
        GameBoard.Instance.StartCoroutine(ParticleManager.Instance.PlayUltimateWhiteGlow(transform.position, visualDelay));
        GameBoard.Instance.StartCoroutine(ParticleManager.Instance.PlayWhiteCubeDestructionEffect(transform.position, visualDelay));

        // Get the width and height of the current level
        int width = LevelManager.Instance.GetGridWidth();
        int height = LevelManager.Instance.GetGridHeight();

        // Create a list to hold the cubes to pull
        List<Cube> cubesToPull = new();

        // Loop through each tile in the grid to find cubes of the selected colors
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Get the board tile at the current indices
                var tile = GameBoard.Instance.GetBoardTileAt(x, y);
                // If the tile is not empty,
                if (!tile.GetEmpty())
                {
                    // Get the object inside the tile
                    GameObject obj = tile.GetObjectInside();
                    // If the object is a Cube, check its color
                    if (obj != null && obj.TryGetComponent<Cube>(out Cube cube))
                    {
                        // If the cube's color is in the selected colors, add it to the cubes to pull list
                        CubeColor c = cube.GetColor();
                        if (selectedColors.Contains(c))
                        {
                            cubesToPull.Add(cube);
                        }
                    }
                    // If the object is null or not a Cube, continue to the next tile
                    else
                    {
                        continue;
                    }

                }
            }
        }

        // Wait for the visual delay before pulling the cubes
        yield return new WaitForSeconds(visualDelay);

        // The duration to pull the cubes to the White Cube's position
        float moveDuration = 0.5f;

        // Reset the match state of the cubes to pull, and move them to the white cube's position
        foreach (Cube cube in cubesToPull)
        {
            // Move the cube to the White Cube's position and despawn it after the move
            StartCoroutine(cube.MoveToAndDespawn(transform.localPosition, moveDuration));
        }

        // Wait for the cubes to finish moving
        yield return new WaitForSeconds(moveDuration);

        // Play the pop animation for this White Cube
        yield return StartCoroutine(PlayPopAnimation(1.6f, 0.25f));

        // Then despawn this White Cube
        ObjectUtils.SafeDespawn(this);
        // Reset the sorting order of the SortingGroup component
        group.sortingOrder = originalOrder;
        // Reset the transform scale to normal
        transform.localScale = Vector3.one;

        // Increment the combo count and show the combo text at this White Cube's position
        ComboManager.Instance.IncrementCombo();
        ComboManager.Instance.ShowComboTextAt(transform);

        // Wait until all particle effects are done playing and all cubes have finished moving
        yield return new WaitUntil(() => ParticleManager.Instance.AreAllEffectsDone());
        yield return new WaitUntil(() => GameBoard.Instance.AllCubesFinishedMoving());
    }

    /// <summary>
    /// Moves the White Cube to the target position with an animation over the specified duration.
    /// </summary>
    /// <param name="targetPos">The target position as 2D Vector.</param>
    /// <param name="duration">The duration of the move, by default is 0.4 seconds.</param>
    public IEnumerator MoveToPositionAnimated(Vector2 targetPos, float duration = 0.4f)
    {
        // Set the initial position and elapsed time
        Vector2 startPos = transform.localPosition;
        float elapsed = 0f;

        // Loop until the elapsed time is less than the duration
        while (elapsed < duration)
        {
            // Lerp the position from start to target based on the elapsed time
            transform.localPosition = Vector2.Lerp(startPos, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        // Set the final position to ensure it matches the target position
        transform.localPosition = targetPos;
    }
}
