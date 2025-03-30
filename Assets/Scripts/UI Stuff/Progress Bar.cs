using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProgressBar : MonoBehaviour
{
    [SerializeField] private GameObject _negativeBarAsChildOfPositiveBar;

    public void DisplayProgress(float value)
    {
        if (value < 0) value = 0;
        _negativeBarAsChildOfPositiveBar.transform.localScale = new(1 - value, 1, 1);
        _negativeBarAsChildOfPositiveBar.transform.localPosition = new(0.5f - _negativeBarAsChildOfPositiveBar.transform.localScale.x / 2, 0, 0);
    }
}
