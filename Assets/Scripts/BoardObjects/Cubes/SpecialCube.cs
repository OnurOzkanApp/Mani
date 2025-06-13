using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SpecialCube : Cube
{
    public abstract IEnumerator TriggerSpecialEffect(Cube cubeToIgnore = null);
}
