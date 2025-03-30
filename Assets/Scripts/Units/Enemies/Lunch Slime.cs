/*
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
*/

using UnityEngine;

public class LunchSlime : Enemy
{
    protected override void Awake()
    {
        base.Awake();
        _rng = 1; 
        _mov = 5;
        _atk = 7;
        _spd = 5;
        _hp = 15;
        _maxHP = _hp;
        _def = 2;

#warning temporary
        _lvl = 1;

        _hpG = 0.5f;
        _atkG = 0.4f;
        _spdG = 0.35f;
        _defG = 0.4f;

        _baseExp = 34;

        _unitType = UnitType.LunchSlime;
        _unitName = "Lunch Slime";

    }

}
