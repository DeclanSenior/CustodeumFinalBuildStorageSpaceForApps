using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnpauseButton : Button
{
    public static event Action UnpauseClicked;

    protected override void OnMouseUpAsButton()
    {
        OnMouseExit();
        OnClick();
    }

    protected override void OnClick()
    {
        Debug.Log("Clicked Unpause Button");
        UnpauseClicked?.Invoke();
    }
}
