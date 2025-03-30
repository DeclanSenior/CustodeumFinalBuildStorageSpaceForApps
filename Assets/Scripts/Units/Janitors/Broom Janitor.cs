using UnityEngine;

public class BroomJanitor : Janitor
{
    protected override void Awake()
    {
        base.Awake();
        _rng = 1;
        _mov = 7;
        _atk = 6;
        _spd = 10;
        _hp = 18;
        _maxHP = _hp;
        _def = 3;

#warning temporary
        _lvl = 1;

        //Whenever you change these, change the InitUnit function in Unit class accordingly
        _hpG = 0.75f;
        _atkG = 0.70f;
        _spdG = 0.75f;
        _defG = 0.65f;

        _unitType = UnitType.BroomJanitor;
        _unitName = "Broom Janitor";
    }
}
