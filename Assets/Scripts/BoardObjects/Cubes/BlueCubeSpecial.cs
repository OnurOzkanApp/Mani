using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlueCubeSpecial : SpecialCube
{
    /// <summary>
    /// Triggers the Special Cube effect for the Special Blue Cube. It destroys all the
    /// cubes that are on the same column as this Special Blue Cube.
    /// <param name="cubeToIgnore">The cube to ignore destroying, by default is is set as null.</param>
    public override IEnumerator TriggerSpecialEffect(Cube cubeToIgnore = null)
    {
        // Get the X index of this Special Blue Cube
        int x = GetX();
        // Get the height of the current board from the Level Manager
        int height = LevelManager.Instance.GetGridHeight();

        // Find the local position of the Waterleak effect
        Vector2 localPos = GameBoard.Instance.FindPositionsGivenIndices(x, height - 2);
        // Find the spawn position of the effect
        Vector3 spawnPos = GameBoard.Instance.transform.TransformPoint(localPos);
        // Adjust the Y position of the effect to play to be just above the last row
        spawnPos.y -= 0.24f;

        // Cubes and Obstacles to target in the same column of this Special Blue Cube
        List<Cube> cubeTargets = new List<Cube>();
        List<Obstacle> obstacleTargets = new List<Obstacle>();

        // Loop through the column
        for (int j = 0; j < height; j++)
        {
            // Get the object inside Board Tile at the coordinates
            GameObject obj = GameBoard.Instance.GetBoardTileAt(x, j).GetObjectInside();
            // If the object inside is a cube, it is not this, and it is not the cube to ignore, add it to target list
            if (obj != null && obj.TryGetComponent<Cube>(out Cube targetCube))
            {
                if (targetCube != null && targetCube != cubeToIgnore && targetCube.gameObject.activeInHierarchy)
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

        // Play the special cube effect for the Special Blue Cube
        ParticleManager.Instance.PlaySpecialCubeEffect(CubeColor.Blue, spawnPos, Vector2.zero);
        yield return new WaitForSeconds(ParticleManager.Instance.GetWaitTimeForSpecialCube());

        // Then loop through the targeted cubes in the same column and despawn them
        foreach (Cube c in cubeTargets)
        {
            ObjectUtils.SafeDespawn(c);
        }
        // Also loop through the targeted obstacles in the same column and hit them once
        foreach (Obstacle o in obstacleTargets)
        {
            if (o != null && o.gameObject.activeInHierarchy)
            {
                yield return new WaitForSeconds(0.05f);
                GameBoard.Instance.StartCoroutine(o.TakeHit());
            }
        }
        // Despawn the Special Blue Cube
        ObjectUtils.SafeDespawn(this);
        // Add this Special Blue Cube to the cube Targets and do damage to Obstacles around the destroyed cubes
        cubeTargets.Add(this);
        GameBoard.Instance.DamageAdjacentObstacles(cubeTargets);
    }
}
