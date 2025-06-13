using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a Prism Obstacle that breaks after one hit.
/// Contains animation support for shattering before despawning.
/// </summary>
public class Prism : Obstacle
{
    [Header("Prism Settings")]
    [Tooltip("Total number of hits required to destroy the prism (default 1).")]
    [SerializeField] private int hitPoints = 1;

    /// <summary>
    /// Sets the HP of the Prism when enabled.
    /// </summary>
    private void OnEnable()
    {
        // Default HP of the Prism
        hitPoints = 1;
        // Ensure the Prism sprite is visible when reused
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.enabled = true;
    }

    /// <summary>
    /// Returns the number of remaining hit points for the Prism.
    /// </summary>
    public override int GetRemainingHitPoints()
    {
        return hitPoints;
    }

    /// <summary>
    /// Applies damage to this Prism. Plays the shatter effect and despawns the Prism.
    /// </summary>
    public override IEnumerator TakeHit()
    {
        hitPoints--;

        if (hitPoints <= 0)
        {
            // Hide the visual immediately
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.enabled = false;

            // Fire-and-forget the visual shatter (no yield)
            GameBoard.Instance.StartCoroutine(ShatterAndDestroy());

            // Immediately despawn the obstacle (frees tile for drops)
            GameBoard.Instance.DespawnObstacle(this);
        }

        yield break;
    }

    /// <summary>
    /// Shatters the Prism into multiple pieces by spawning 5 pieces of the Prism from the
    /// object pool. Then despawns the Prism and updates the Board.
    /// </summary>
    public IEnumerator ShatterAndDestroy()
    {
        Vector3 pos = transform.position;
        Vector3 boardScale = GameBoard.Instance.transform.localScale;

        string[] shardKeys = {
        "PrismShardOne", "PrismShardTwo", "PrismShardThree", "PrismShardFour", "PrismShardFive"
    };

        foreach (string key in shardKeys)
        {
            GameObject shard = ObjectPoolManager.SpawnObjectByKey(key, pos, inBoard: false);
            if (shard != null)
            {
                shard.transform.localScale = boardScale;
                shard.transform.SetParent(null);

                var effect = shard.GetComponent<ShatterEffect>();
                if (effect != null)
                {
                    effect.Play();
                }
            }
        }

        yield break;
    }

    /// <summary>
    /// Delays the despawn to match animation length, if used.
    /// </summary>
    private IEnumerator DelayedDespawn()
    {
        yield return new WaitForSeconds(0.5f); // match animation duration
        ObjectUtils.SafeDespawn(gameObject);
    }
}
