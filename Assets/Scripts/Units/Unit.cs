using System;
//using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

public abstract class Unit : MonoBehaviour
{
    protected Team _team;
    protected UnitType _unitType;

    protected string _unitName;

    protected int _hp, _maxHP, _atk, _def, _spd, _mov, _rng, _lvl, _exp, _baseExp; //basic stats
    protected float _hpG, _atkG, _defG, _spdG; //growth rates upon level up

    protected bool _hasMoved; //Turns true once a unit has acted
    private bool _inPosition;
    private Vector3Int _position;
    private Vector3Int? _targetPosition;
    private List<Vector3> _path;
    private (List<Vector3Int>, List<Vector3Int>) _moveableAndAttackSquares;
    private int _currentPathIndex;

    private Vector3Int _originalPosition;

    private GameObject _attackTarget;

    private AnimationClip _attackAnimation;

    private GameObject _miniHealthBarGreen;

    public void SetMiniHealthBarGreen(GameObject bar, GameObject parent)
    {
       _miniHealthBarGreen = bar;

        _miniHealthBarGreen.transform.SetParent(parent.transform);
    }
    public void UpdateMiniHealthBar()
    {
        _miniHealthBarGreen.GetComponent<ProgressBar>().DisplayProgress((float)_hp / (float)_maxHP);
    }
    public void HideMiniHealthBar()
    {
        _miniHealthBarGreen.SetActive(false);
    }
    public void ShowMiniHealthBar()
    {
        _miniHealthBarGreen.SetActive(true);
    }

    public void SetPosition(Vector3Int position) { _position = position; }
    public Vector3Int GetPosition() { return _position; }
    public void SetMoveableAndAttackableTiles((List<Vector3Int>, List<Vector3Int>) listPair) { _moveableAndAttackSquares = listPair; }
    public List<Vector3Int> GetMoveableTiles() { return _moveableAndAttackSquares.Item1; }
    public List<Vector3Int> GetAttackableOnlySquares() { return _moveableAndAttackSquares.Item2; }
    public List<Vector3Int> GetMoveableAndAttackableSquares()
    {
        List<Vector3Int> tempList = _moveableAndAttackSquares.Item1;
        tempList.AddRange(_moveableAndAttackSquares.Item2);
        return tempList;
    }
    public bool IsInPosition() { return _inPosition; }
    public void SetInPosition(bool value) { _inPosition = value; }
    public Vector3Int? GetTargetPosition() { return _targetPosition; }
    public void SetTargetPosition(Vector3Int? value) { _targetPosition = value; }
    public List<Vector3> GetPath() { return _path; }
    public void SetPath(List<Vector3> path) { _path = path; }
    public void SetCurrentPathIndex(int index) { _currentPathIndex = index; }
    public int GetCurrentPathIndex() { return _currentPathIndex; }
    public void SetOriginalPosition(Vector3Int position) { _originalPosition = position; }
    public Vector3Int GetOriginalPosition() { return _originalPosition; }
    public int GetMov() { return _mov; }
    public int GetRng() { return _rng; }

    public int GetMaxHP() { return _maxHP; }
    public int GetHp() { return _hp; }
    public int GetAtk() { return _atk; }
    public int GetDef() { return _def; }
    public int GetSpd() { return _spd; }
    public string GetUnitName() { return _unitName; }
    public UnitType GetUnitType() { return _unitType; }
    public Team GetTeam() { return _team; }
    public bool IsWaiting() { return _hasMoved; }
    public void SetIsWaiting(bool value) { _hasMoved = value; }

    public void SetAttackTarget(GameObject target) { _attackTarget = target; }
    public GameObject GetAttackTarget() { return _attackTarget; }
    public float GetHPPercent()
    {
        return (float) _hp / (float) _maxHP;
    }

    public int GetExp() {  return _exp; }

    public int GetBaseExp() {  return _baseExp; }

    public int GetLvl() { return _lvl; }

    public bool ReduceHp(int amount) 
    {
        this._hp -= amount;

        if (this._hp <= 0) return true;
        else return false;
    }
    public AttackResults AttackedBy(Unit attackerUnit, int rng) 
    {
        int dmgAmount = attackerUnit.GetAtk() - this._def;
        if (dmgAmount < 0) dmgAmount = 0;

        int counterDmgAmount = 0;

        if (rng <= this._rng)
        {
            counterDmgAmount = this._atk - attackerUnit.GetDef();
            if (counterDmgAmount < 0) counterDmgAmount = 0;
        }

        if (this.ReduceHp(dmgAmount)) return AttackResults.Kill;
        
        else 
        {
            if (attackerUnit.ReduceHp(counterDmgAmount)) return AttackResults.CounterKill;

            if (this._spd - attackerUnit.GetSpd() >= 5)
            {
                if (attackerUnit.ReduceHp(counterDmgAmount)) return AttackResults.CounterKill;
            }
        }

        if (attackerUnit.GetSpd() - this._spd >= 5)
        {
            if (this.ReduceHp(dmgAmount)) return AttackResults.Kill;
        }

        return AttackResults.NoKill;

    }


    public void AddEXPAndLevelUpIfNecessary(int AddedExp)
    {
        
        int TotalExp = AddedExp + _exp;

        int numLvlUps = 0;

        for (int i = 0; i < (int)(TotalExp / 100); i++)
        {
            numLvlUps++;
        }

        for (int i = 0; i < numLvlUps; i++)
        {
            LvlUp();
        }

        _exp = TotalExp % 100;
        
    }

    public void LvlUp()
    {
        System.Random rand = new System.Random();
        
        if (rand.NextDouble() <= _hpG) _maxHP++;
        if (rand.NextDouble() <= _atkG) _atk++;
        if (rand.NextDouble() <= _defG) _def++;
        if (rand.NextDouble() <= _spdG) _spd++;

        

        _lvl++;

    }

    public void SimLevelUp(int toLevel)
    {
        _atk += (int)(_atkG * (toLevel - 1));
        _def += (int)(_defG * (toLevel - 1));
        _spd += (int)(_spdG * (toLevel - 1));
        _maxHP += (int)(_hpG * (toLevel - 1));
        _hp = _maxHP;
        _lvl = toLevel;
    }

    public void SetStats(int maxHp, int atk, int def, int spd, int exp, int lvl, UnitType unitType)
    {
        _hp = maxHp;
        _maxHP = maxHp;
        _atk = atk;
        _def = def;
        _spd = spd;
        _exp = exp;
        _lvl = lvl;

        _unitType = unitType;

        switch (unitType)
        {
            case UnitType.MopJanitor:

                _hpG = 0.8f;
                _atkG = 0.8f;
                _spdG = 0.55f;
                _defG = 0.7f;

                _rng = 1;
                _mov = 6;

                break;
            case UnitType.BroomJanitor:

                _hpG = 0.75f;
                _atkG = 0.70f;
                _spdG = 0.75f;
                _defG = 0.65f;

                _rng = 1;
                _mov = 7;

                break;
            case UnitType.VacuumJanitor:

                _hpG = 0.75f;
                _atkG = 0.7f;
                _spdG = 0.55f;
                _defG = 0.55f;

                _rng = 2;
                _mov = 6;

                break;
            case UnitType.SprayBottleJanitor:

                _hpG = 0.7f;
                _atkG = 0.65f;
                _spdG = 0.7f;
                _defG = 0.50f;

                _rng = 3;
                _mov = 6;

                break;
            default: break;

        }
    }

    public void SetStatsBasedOnUnit(Unit unit)
    {
        _hp = unit.GetMaxHP();
        _maxHP = unit.GetMaxHP();
        _atk = unit.GetAtk();
        _def = unit.GetDef();
        _spd = unit.GetSpd();
        _rng = unit.GetRng();
        _mov = unit.GetMov();
        _exp = unit.GetExp();
        _lvl = unit.GetLvl();


        UnitType unitType = unit.GetUnitType();

        _unitType = unitType;

        switch (unitType)
        {
#warning ok this seems like a really obtuse way of doing things but it works
            case UnitType.MopJanitor:
                _hpG = 0.8f;
                _atkG = 0.8f;
                _spdG = 0.55f;
                _defG = 0.7f;
                break;
            case UnitType.BroomJanitor:
                _hpG = 0.75f;
                _atkG = 0.70f;
                _spdG = 0.75f;
                _defG = 0.65f;
                break;
            case UnitType.SprayBottleJanitor:
                _hpG = 0.7f;
                _atkG = 0.65f;
                _spdG = 0.7f;
                _defG = 0.50f;
                break;
            case UnitType.VacuumJanitor:
                _hpG = 0.75f;
                _atkG = 0.7f;
                _spdG = 0.55f;
                _defG = 0.55f;
                break;
            default: break;

        }


    }


}

public enum Team
{
    Janitor,
    Enemy,
    Passive,
}

public enum UnitType
{
    MopJanitor,
    BroomJanitor,
    VacuumJanitor,
    SprayBottleJanitor,

    LunchSlime,
    PaperCrane,
    DirtBall,
    RedSlushie,
    GreenSlushie

}

public enum AttackResults
{
    Kill,
    NoKill,
    CounterKill
}

