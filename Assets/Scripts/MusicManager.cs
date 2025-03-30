using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    [SerializeField] AudioSource _source;

    private void OnEnable()
    {
        UnitManager.PlayMusic += PlayMusic;
    }

    private void OnDisable()
    {
        UnitManager.PlayMusic += PlayMusic;
    }

    private void PlayMusic(LevelData level)
    {
        _source.clip = level.Music;
        _source.volume = level.MusicVolume;

        _source.Play();
    }


}
