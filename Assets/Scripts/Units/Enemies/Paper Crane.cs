using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaperCrane : Enemy
{

    protected override void Awake()
    {
        base.Awake();
        _rng = 1;
        _mov = 6;
        _atk = 6;
        _spd = 10;
        _hp = 13;
        _maxHP = _hp;
        _def = 1;

#warning temporary
        _lvl = 1;

        _hpG = 0.40f;
        _atkG = 0.4f;
        _spdG = 0.55f;
        _defG = 0.4f;

        _baseExp = 40;

        _unitType = UnitType.PaperCrane;
        _unitName = "Paper Crane";

    }
}
