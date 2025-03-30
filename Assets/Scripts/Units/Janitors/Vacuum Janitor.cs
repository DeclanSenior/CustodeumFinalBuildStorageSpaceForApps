using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VacuumJanitor : Janitor
{
    protected override void Awake()
    {
        base.Awake();
        _rng = 2;
        _mov = 6;
        _atk = 8;
        _spd = 7;
        _hp = 17;
        _maxHP = _hp;
        _def = 3;

#warning temporary
        _lvl = 1;

        //Whenever you change these, change the InitUnit function in Unit class accordingly
        _hpG = 0.75f;
        _atkG = 0.7f;
        _spdG = 0.55f;
        _defG = 0.55f;

        _unitType = UnitType.VacuumJanitor;
        _unitName = "Vacuum Janitor";
    }
}

