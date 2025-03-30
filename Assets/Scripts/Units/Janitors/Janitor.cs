/*
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
*/

public abstract class Janitor : Unit
{
    protected virtual void Awake()
    {
        _team = Team.Janitor;
    }
}
