using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RedCubeSpecial : SpecialCube
{
    /// <summary>
    /// Triggers the Special Cube effect for the Special Red Cube. It destroys all the
    /// cubes that are on the same row as this Special Red Cube.
    /// <param name="cubeToIgnore">The cube to ignore destroying, by default is is set as null.</param>
    public override IEnumerator TriggerSpecialEffect(Cube cubeToIgnore = null)
    { 
        // Get the Y index of this Special Red Cube
        int y = GetY();
        // Get the width of the current board from the Level Manager
        int width = LevelManager.Instance.GetGridWidth();

        // Cubes and Obstacles to target in the same row of this Special Red Cube
        List<Cube> cubeTargets = new List<Cube>();
        List<Obstacle> obstacleTargets = new List<Obstacle>();

        // Loop through the row
        for (int i = 0; i < width; i++)
        {
            // Get the object inside Board Tile at the coordinates
            GameObject obj = GameBoard.Instance.GetBoardTileAt(i, y).GetObjectInside();
            // If the object inside is a cube, it is not this, and it is not the cube to ignore, add it to target list
            if (obj != null && obj.TryGetComponent<Cube>(out Cube targetCube))
            {
                if (targetCube != null && targetCube != this && targetCube != cubeToIgnore && targetCube.gameObject.activeInHierarchy)
                {
                    cubeTargets.Add(targetCube);
                    ParticleManager.Instance.PlaySpecialCubeEffect(CubeColor.Red, targetCube.transform.position, Vector2.zero);
                }
            }
            // If the object inside is an Obstacle, add it to target list
            else if (obj != null && obj.TryGetComponent<Obstacle>(out Obstacle targetObstacle))
            {
                obstacleTargets.Add(targetObstacle);
            }
        }

        // Play the special cube effect for the Special Red Cube
        ParticleManager.Instance.PlaySpecialCubeEffect(CubeColor.Red, transform.position, Vector2.zero);
        yield return new WaitForSeconds(ParticleManager.Instance.GetWaitTimeForSpecialCube());

        // Then loop through the targeted cubes in the same row and despawn them
        foreach (Cube c in cubeTargets)
        {
            ObjectUtils.SafeDespawn(c);
        }
        // Also loop through the targeted obstacles in the same row and hit them once
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
