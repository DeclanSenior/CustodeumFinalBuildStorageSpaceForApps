using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResumeButton : Button
{
    [SerializeField] private GameObject _fadeToBlackObject;
    [SerializeField] private GameObject _resumePanel;

    private bool _panelUp = false;
    protected override void OnClick()
    {
        if (_panelUp)
        {
            _resumePanel.SetActive(false);
            _panelUp = false;
        }
        else
        {
            _resumePanel.SetActive(true);
            _panelUp = true;
        }
    }

}
