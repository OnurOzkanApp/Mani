using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public class YellowCubeSpecial : SpecialCube
{
    /// <summary>
    /// Triggers the Special Cube effect for the Special Yellow Cube. It selects and destroys
    /// 5 random Board Objects on the board. However, if it selects an Obstacle, it uses zaps
    /// until the Obstacle gets destroyed. Hence, seeing less than 5 zaps is possible. 
    /// <param name="cubeToIgnore">The cube to ignore destroying, by default is is set as null.</param>
    public override IEnumerator TriggerSpecialEffect(Cube cubeToIgnore = null)
    {
        // Get the width and the height for the current level from Level Manager
        int width = LevelManager.Instance.GetGridWidth();
        int height = LevelManager.Instance.GetGridHeight();

        // Set the zap count and start with 0 zaps
        int totalZaps = 0;
        int zapCount = 5;
        // Create a HashSet to keep track of the positions selected so that we do not select the same position twice
        HashSet<Vector2Int> chosenPositions = new HashSet<Vector2Int>();

        // Cubes and Obstacles that were chosen randomly by this Special Yellow Cube
        List<Cube> cubeTargets = new List<Cube>();
        List<(Obstacle obstacle, int hitCount)> obstacleTargets = new List<(Obstacle, int)>();

        // Try to find 5 different targets 100 times and break out when we find 5 targets
        int attempts = 0;
        while (totalZaps < zapCount && attempts < 200)
        {
            // Randomize X and Y indices on the board
            int randX = Random.Range(0, width);
            int randY = Random.Range(0, height);
            Vector2Int pos = new Vector2Int(randX, randY);

            // Check if we have already selected that position, if so, try again
            if (chosenPositions.Contains(pos))
            {
                attempts++;
                continue;
            }

            // Otherwise, get the object inside the tile, if the tile is empty, try again
            GameObject obj = GameBoard.Instance.GetBoardTileAt(randX, randY)?.GetObjectInside();
            if (obj == null)
            {
                attempts++;
                continue;
            }
            // Create a flag to keep track if the target is accepted byt the zap mechanic
            bool accepted = false;
            // If the object inside the tile is a Cube,
            if (obj.TryGetComponent(out Cube cubeTarget))
            {
                // Check if it is not white, not this Special Yellow Cube, it doesn't have a match and is active
                bool validCube = cubeTarget != this
                    && cubeTarget != cubeToIgnore
                    && cubeTarget.GetColor() != CubeColor.White
                    && cubeTarget.gameObject.activeInHierarchy
                    && !cubeTarget.HasMatch;

                // If all conditions are met, set that we found a valid Cube target and add it to target cubes
                if (validCube)
                {
                    cubeTargets.Add(cubeTarget);
                    totalZaps += 1;
                    accepted = true;
                }
            }
            // If the object inside the tile is an Obstacle
            else if (obj.TryGetComponent(out Obstacle obstacleTarget))
            {
                // Zap the Obstacle until it gets destroyed while we have enough zaps
                int hp = obstacleTarget.GetRemainingHitPoints();
                if (hp > 0)
                {
                    obstacleTargets.Add((obstacleTarget, hp));
                    totalZaps += hp;
                    accepted = true;
                }
            }
            // If the target were accepted, add it to chosen positions to not choose it again
            if (accepted)
            {
                chosenPositions.Add(pos);
            }
            // Increase the attempt count
            attempts++;
        }

        // Trigger zap visuals for all cube and obstacle targets
        foreach (Cube c in cubeTargets)
        {
            ParticleManager.Instance.PlaySpecialCubeEffect(CubeColor.Yellow, transform.position, c.transform.position);
        }
        foreach (var (obstacle, hits) in obstacleTargets)
        {
            // Hit the Obstacle until it gets destroyed
            for (int i = 0; i < hits; i++)
            {
                ParticleManager.Instance.PlaySpecialCubeEffect(CubeColor.Yellow, transform.position, obstacle.transform.position);
            }
        }

        // Wait for all zap visuals to complete
        yield return new WaitForSeconds(ParticleManager.Instance.GetWaitTimeForSpecialCube());

        // Despawn cube targets AFTER effect has fully played
        foreach (Cube c in cubeTargets)
        {
            ObjectUtils.SafeDespawn(c);
        }
        // Damage obstacles AFTER zap
        foreach (var (obstacle, hits) in obstacleTargets)
        {
            for (int i = 0; i < hits; i++)
            {
                if (obstacle != null && obstacle.gameObject.activeInHierarchy)
                {
                    yield return new WaitForSeconds(0.05f);
                    GameBoard.Instance.StartCoroutine(obstacle.TakeHit());
                }
            }
        }
        // Damage adjacent obstacles
        cubeTargets.Add(this);
    }
}
