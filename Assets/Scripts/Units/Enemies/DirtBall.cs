/*
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
*/

using UnityEngine;

public class DirtBall : Enemy
{
    protected override void Awake()
    {
        base.Awake();
        _rng = 1;
        _mov = 2;
        _atk = 14;
        _spd = 2;
        _hp = 25;
        _maxHP = _hp;
        _def = 5;

#warning temporary
        _lvl = 1;

        _hpG = 0.6f;
        _atkG = 0.5f;
        _spdG = 0.2f;
        _defG = 0.6f;

        _baseExp = 50;

        _unitType = UnitType.DirtBall;
        _unitName = "Dirt Ball";

    }

}
