/*
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
*/

public abstract class Enemy : Unit
{
    protected virtual void Awake()
    {
        _team = Team.Enemy;
    }
}

