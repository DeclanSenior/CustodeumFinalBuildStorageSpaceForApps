using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public static event Action DialogueDone;

    [SerializeField] private GameObject _textBoxSpeaker;
    [SerializeField] private GameObject _textBoxBody;

    private bool _nextJustPressed = false;

    private void OnEnable()
    {
        UnitManager.DisplayLevelDialogueEvent += StartDisplayLevelText;
        NextButton.NextButtonPressed += NextPressed;
    }

    private void OnDisable()
    {
        UnitManager.DisplayLevelDialogueEvent -= StartDisplayLevelText;
        NextButton.NextButtonPressed -= NextPressed;
    }

    private void StartDisplayLevelText(LevelData level)
    {
        StartCoroutine(DisplayLevelText(level));
    }

    private void NextPressed()
    {
        _nextJustPressed = true;
    }

    private IEnumerator DisplayLevelText(LevelData level)
    {
        GameManager.Instance.enabled = false;

        for (int i = 0; i < level.levelText.Length; i++)
        {
            _textBoxSpeaker.GetComponent<TextMeshProUGUI>().text = level.levelTextSpeaker[i];
            _textBoxBody.GetComponent<TextMeshProUGUI>().text = level.levelText[i];

            yield return new WaitUntil(() => _nextJustPressed);
            _nextJustPressed = false;
        }

        GameManager.Instance.enabled = true;

        _textBoxSpeaker.GetComponent<TextMeshProUGUI>().text = "";
        _textBoxBody.GetComponent<TextMeshProUGUI>().text = "";

        DialogueDone?.Invoke();
    }

}
