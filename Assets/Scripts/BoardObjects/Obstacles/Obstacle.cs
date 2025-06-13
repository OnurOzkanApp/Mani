using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Obstacle : BoardObject
{
    public virtual bool ShouldFall() => true;

    public abstract IEnumerator TakeHit();

    public abstract int GetRemainingHitPoints();
}

