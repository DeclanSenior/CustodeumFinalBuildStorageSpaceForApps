/*
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
*/

using UnityEngine;

public class GreenSlushie : Enemy
{
    protected override void Awake()
    {
        base.Awake();
        _rng = 2;
        _mov = 5;
        _atk = 7;
        _spd = 5;
        _hp = 13;
        _maxHP = _hp;
        _def = 2;

#warning temporary
        _lvl = 1;

        _hpG = 0.4f;
        _atkG = 0.4f;
        _spdG = 0.3f;
        _defG = 0.2f;

        _baseExp = 33;

        _unitType = UnitType.GreenSlushie;
        _unitName = "Green Slushie";

    }

}
