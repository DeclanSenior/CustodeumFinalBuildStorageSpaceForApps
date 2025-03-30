using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class NewGameButton : Button
{
    [SerializeField] private GameObject _fadeToBlackObject;

    [SerializeField] private TMP_InputField _inputField;
    public static string saveDataString = string.Empty;

    protected override void OnClick()
    {
        if (_inputField != null) saveDataString = _inputField.text;
        StartCoroutine(FadeInThenLoadScene(_fadeToBlackObject, "Battle Scene"));
    }

    private IEnumerator FadeInThenLoadScene(GameObject targetObject, string sceneName)
    {
        SpriteRenderer renderer = targetObject.GetComponent<SpriteRenderer>();

        for (float i = 0; i < 1f; i += 0.05f)
        {
            renderer.color = new Color(0, 0, 0, i);

            yield return null;
        }

        renderer.color = new Color(0, 0, 0, 1f);

        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);

    }
}
