using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlackCubeSpecial : SpecialCube
{
    /// <summary>
    /// Triggers the Special Cube effect for the Special Black Cube. It destroys a 3x3
    /// area, the Black Special Cube being in the center.
    /// </summary>
    /// <param name="cubeToIgnore">The cube to ignore destroying, by default is is set as null.</param>
    public override IEnumerator TriggerSpecialEffect(Cube cubeToIgnore = null)
    { 
        // Get the center cube indices
        int centerX = GetX();
        int centerY = GetY();

        // Cubes and Obstacles to target in the 3x3 area of this Special Black Cube
        List<Cube> cubeTargets = new List<Cube>();
        List<Obstacle> obstacleTargets = new List<Obstacle>();

        // Loop through the 3x3 area
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                // Update the indices
                int x = centerX + dx;
                int y = centerY + dy;

                // Get the Board Tile at the coordinates
                BoardTile tile = GameBoard.Instance.GetBoardTileAt(x, y);
                if (tile != null && !tile.GetEmpty())
                {
                    // If the tile is not empty, get the object inside the tile
                    GameObject obj = tile.GetObjectInside();
                    // If the object inside is a cube, it is not this, and it is not the cube to ignore, add it to target list
                    if (obj != null && obj.TryGetComponent<Cube>(out Cube targetCube))
                    {
                        if (targetCube != this && targetCube != cubeToIgnore && targetCube.gameObject.activeInHierarchy)
                        {
                            cubeTargets.Add(targetCube);
                        }
                    }
                    // If the object inside is an Obstacle, add it to target list
                    else if (obj != null && obj.TryGetComponent<Obstacle>(out Obstacle targetObstacle))
                    {
                        obstacleTargets.Add(targetObstacle);
                    }
                }
            }
        }

        // Play the special cube effect for the Special Black Cube
        ParticleManager.Instance.PlaySpecialCubeEffect(CubeColor.Black, transform.position, Vector2.zero);
        yield return new WaitForSeconds(ParticleManager.Instance.GetWaitTimeForSpecialCube());

        // Then loop through the targeted cubes in the 3x3 area and despawn them
        foreach (Cube c in cubeTargets)
        {
            ObjectUtils.SafeDespawn(c);
        }
        // Also loop through the targeted obstacles in the 3x3 area and hit them once
        foreach (Obstacle o in obstacleTargets)
        {
            if (o != null && o.gameObject.activeInHierarchy)
            {
                yield return new WaitForSeconds(0.05f);
                GameBoard.Instance.StartCoroutine(o.TakeHit());
            }
        }
        // Add this Special Black Cube to the cube Targets and do damage to Obstacles around the destroyed cubes
        cubeTargets.Add(this);
        GameBoard.Instance.DamageAdjacentObstacles(cubeTargets);
    }
}
