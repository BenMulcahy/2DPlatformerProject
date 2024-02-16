using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IHittable
{
    void TakeDamage(float damage);
    bool bHasBeenHitThisInstance { get; set; }
}
