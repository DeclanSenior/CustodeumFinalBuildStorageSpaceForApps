using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationUtils : MonoBehaviour
{
    public static event Action EndAnimationEvent;
    public static event Action AttackConnectedEvent;
    public static event Action<string> SoundEffectUnitTypeNameEvent;

    public void AttackComplete()
    {
        EndAnimationEvent?.Invoke();
    }

    public void AttackConnected()
    {
        AttackConnectedEvent?.Invoke();
    }

    public void PlaySound(string unitTypeName)
    {
        SoundEffectUnitTypeNameEvent?.Invoke(unitTypeName);
    }
}
