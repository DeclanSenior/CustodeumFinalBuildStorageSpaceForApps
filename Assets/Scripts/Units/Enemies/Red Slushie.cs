/*
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
*/

using UnityEngine;

public class RedSlushie : Enemy
{
    protected override void Awake()
    {
        base.Awake();
        _rng = 3;
        _mov = 4;
        _atk = 5;
        _spd = 4;
        _hp = 10;
        _maxHP = _hp;
        _def = 2;

#warning temporary
        _lvl = 1;

        _hpG = 0.35f;
        _atkG = 0.35f;
        _spdG = 0.25f;
        _defG = 0.2f;

        _baseExp = 33;

        _unitType = UnitType.RedSlushie;
        _unitName = "Red Slushie";

    }

}
