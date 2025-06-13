using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Utility class for safely despawning objects on the board.
/// Handles Cubes, Obstacles, and general pooled objects.
/// </summary>
public static class ObjectUtils
{
    /// <summary>
    /// Safely despawns an object from the board if it is active.
    /// Checks if the object is a Cube or Obstacle and uses the appropriate despawn method.
    /// </summary>
    /// <param name="obj">The Game Object to despawn.</param>
    /// <param name="decreaseTargetCount">The flaf for whether to decrease the target count for this object.</param>
    public static void SafeDespawn(GameObject obj, bool decreaseTargetCount = true)
    {
        if (obj == null || !obj.activeInHierarchy)
            return;

        // First check if the given object is a Cube, if so, despawn it while decreasing its target count
        if (obj.TryGetComponent<Cube>(out Cube cube))
        {
            GameBoard.Instance.DespawnCube(cube, decreaseTargetCount);
            return;
        }

        // Then check if it is an Obstacle, and do the same
        if (obj.TryGetComponent<Obstacle>(out Obstacle obstacle))
        {
            // You may want a DespawnObstacle method, but this fallback works
            GameBoard.Instance.DespawnObstacle(obstacle, decreaseTargetCount);
            return;
        }

        // Default fallback for all other pooled objects
        ObjectPoolManager.DespawnObject(obj);
    }


    /// <summary>
    /// Shortcut for despawning a Cube when there already is a Cube reference.
    /// </summary>
    /// <param name="cube">The Cube object to despawn.</param>
    /// <param name="decreaseTargetCount">The flaf for whether to decrease the target count for this cube.</param>
    public static void SafeDespawn(Cube cube, bool decreaseTargetCount = true)
    {
        if (cube != null && cube.gameObject.activeInHierarchy)
            GameBoard.Instance.DespawnCube(cube, decreaseTargetCount);
    }
}

