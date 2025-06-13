using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShatterEffect : MonoBehaviour
{
    [Tooltip("Duration of the shatter animation.")]
    [SerializeField] private float duration = 0.6f;

    public void Play()
    {
        StartCoroutine(Animate());
    }


    /// <summary>
    /// Damages all adjacent obstacles around the given cubes.
    /// </summary>
    /// <param name="shard">One of the shard pieces Game Object.</param>
    /// <param name="duration">The duration of the shard piece falling and disappearing.</param>
    private IEnumerator Animate()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Vector3 startPos = transform.position;
        Vector3 direction = Random.insideUnitCircle.normalized * 0.4f;
        Vector3 endPos = startPos + direction;

        float elapsed = 0f;
        sr.color = new Color(1, 1, 1, 1);

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.position = Vector3.Lerp(startPos, endPos, t) + new Vector3(0, -1f * t * t, 0);
            sr.color = new Color(1, 1, 1, 1 - t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        ObjectUtils.SafeDespawn(gameObject);
    }
}
