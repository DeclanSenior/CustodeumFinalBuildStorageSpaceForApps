using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagr : MonoBehaviour
{
    public static SceneManager Instance;

    public void FadeOutAndInToScene(string sceneName)
    {
        StartCoroutine(FadeOut(null));
        SceneManager.LoadScene(sceneName);
        StartCoroutine(FadeIn(null));
    }

    IEnumerator FadeOut(GameObject coveringObject)
    {
        yield return null;
    }

    IEnumerator FadeIn(GameObject coveringObject)
    {
        yield return null;
    }
}
