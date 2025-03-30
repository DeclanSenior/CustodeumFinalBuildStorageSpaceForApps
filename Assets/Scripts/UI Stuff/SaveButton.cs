using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveButton : Button
{
    public static event Action SaveButtonPressed;
    protected override void OnClick()
    {
        SaveButtonPressed?.Invoke();
    }
}
