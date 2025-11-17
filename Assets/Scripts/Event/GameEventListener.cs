using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.GameEvents;
using UnityEngine;
using UnityEngine.Events;

public class GameEventListener : MonoBehaviour
{
    [SerializeField] private GameEvent gameEvent;

    public UnityEvent response;

    private void OnEnable()
    {
        if (gameEvent != null)
            gameEvent.Register(this);
    }

    private void OnDisable()
    {
        if (gameEvent != null)
            gameEvent.Unregister(this);
    }

    public void OnEventRaised()
    {
        response?.Invoke();
    }
}