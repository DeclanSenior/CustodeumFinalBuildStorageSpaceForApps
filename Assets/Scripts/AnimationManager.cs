using System.Collections;
using System.Collections.Generic;
using System;

using UnityEngine;
using UnityEngine.Animations;

public class AnimationManager : MonoBehaviour
{
    public static AnimationManager Instance;

    [SerializeField] private List<UnitType> _animationKeys;
    [SerializeField] private List<RuntimeAnimatorController> _animationValuesCorrespondingByIndex;

    private Dictionary<UnitType, RuntimeAnimatorController> _animationPairs;

    // Start is called before the first frame update
    void Awake()
    {
        Instance = this;

        _animationPairs = new Dictionary<UnitType, RuntimeAnimatorController>();

        for (int i = 0; i < _animationKeys.Count; i++)
        {
            _animationPairs.Add(_animationKeys[i], _animationValuesCorrespondingByIndex[i]);
        }
    }

    public RuntimeAnimatorController GetAnimationController(UnitType unitType)
    {
        return _animationPairs[unitType];
    }


}