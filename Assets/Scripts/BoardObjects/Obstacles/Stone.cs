using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a destructible Stone Obstacle that requires multiple hits to be destroyed.
/// Updates its sprite to reflect damage state and despawns itself when hit points reach zero.
/// </summary>
public class Stone : Obstacle
{
    // Prevent the Stone from falling
    public override bool ShouldFall() => false;

    [Header("Stone Settings")]
    [Tooltip("Total number of hits required to destroy the stone.")]
    [SerializeField] private int hitPoints = 2;
    [Tooltip("SpriteRenderer component to update the stone's appearance.")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("Sprite used when the stone is undamaged.")]
    [SerializeField] private Sprite defaultSprite;
    [Tooltip("Sprite used when the stone is damaged (1 HP left).")]
    [SerializeField] private Sprite damagedSprite;

    /// <summary>
    /// Resets the hit points and sprite when the stone is enabled.
    /// </summary>
    private void OnEnable()
    {
        // Set the HP of the Stone as 2
        hitPoints = 2;
        // Set the sprite of the Stone as the initial Stone without damage at the start
        if (spriteRenderer != null && damagedSprite != null)
        {
            spriteRenderer.sprite = defaultSprite;
        }
    }

    /// <summary>
    /// Applies damage to this Stone. Updates its sprite if damaged,
    /// and despawns it if hit points reach zero.
    /// </summary>
    public override IEnumerator TakeHit()
    {
        hitPoints--;

        if (hitPoints == 1)
        {
            if (spriteRenderer != null && damagedSprite != null)
                spriteRenderer.sprite = damagedSprite;
            GameBoard.Instance.StartCoroutine(Shake(0.2f, 0.06f, 0.1f));
        }
        else if (hitPoints <= 0)
        {
            ObjectUtils.SafeDespawn(gameObject);
        }
        yield break;
    }


    /// <summary>
    /// Returns the number of remaining hit points for the stone.
    /// </summary>
    public override int GetRemainingHitPoints()
    {
        return hitPoints;
    }
}
