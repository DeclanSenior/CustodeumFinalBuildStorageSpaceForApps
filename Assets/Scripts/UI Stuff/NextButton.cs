using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NextButton : Button
{
    public static event Action NextButtonPressed;

    protected override void OnClick()
    {
        NextButtonPressed?.Invoke();
    }
}
