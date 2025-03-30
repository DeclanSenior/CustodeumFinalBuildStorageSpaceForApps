using UnityEngine;

public class MopJanitor : Janitor
{
    protected override void Awake()
    {
        base.Awake();
        _rng = 1;
        _mov = 6;
        _atk = 8;
        _spd = 7;
        _hp = 20;
        _maxHP = _hp;
        _def = 3;

#warning temporary
        _lvl = 1;

        //Whenever you change these, change the InitUnit function in Unit class accordingly
        _hpG = 0.8f;
        _atkG = 0.8f;
        _spdG = 0.55f;
        _defG = 0.7f;

        _unitType = UnitType.MopJanitor;
        _unitName = "Mop Janitor";
    }
}
