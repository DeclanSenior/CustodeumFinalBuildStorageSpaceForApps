using UnityEngine;

public class SprayBottleJanitor : Janitor
{
    protected override void Awake()
    {
        base.Awake();
        _rng = 3;
        _mov = 6;
        _atk = 7;
        _spd = 5;
        _hp = 15;
        _maxHP = _hp;
        _def = 2;

#warning temporary
        _lvl = 1;

        //Whenever you change these, change the InitUnit function in Unit class accordingly
        _hpG = 0.7f;
        _atkG = 0.65f;
        _spdG = 0.7f;
        _defG = 0.50f;

        _unitType = UnitType.SprayBottleJanitor;
        _unitName = "Spray Janitor";
    }
}
