using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SFXManager : MonoBehaviour
{
    [SerializeField] private AudioSource _source;

    private List<AudioClip> _audioClips = new();

    private void Awake()
    {
        _audioClips = Resources.LoadAll<AudioClip>("SFX").ToList();
    }

    public void PlayAudio(string soundName)
    {
        if (_audioClips.Count == 0) return;

        _source.clip = _audioClips.First(u => u.name.Equals(soundName));
        _source.pitch = 0.8f + UnityEngine.Random.value * 0.4f;
        _source.volume = 0.7f + UnityEngine.Random.value * 0.3f;
        _source.Play();
    }

    public void PlayAudioOneShot(string soundName)
    {
        if (_audioClips.Count == 0) return;

        _source.pitch = 0.8f + UnityEngine.Random.value * 0.4f;
        _source.volume = 0.7f + UnityEngine.Random.value * 0.3f;
        _source.PlayOneShot(_audioClips.First(u => u.name.Equals(soundName)));
    }


    public void PlayAudioSetPitch(string soundName, float volume, float pitch)
    {
        if (_audioClips.Count == 0) return;

        _source.pitch = pitch;
        _source.volume = volume;

        _source.PlayOneShot(_audioClips.First(u => u.name.Equals(soundName)));
    }

}
