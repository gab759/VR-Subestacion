using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameIntEventListener : MonoBehaviour
{
    [SerializeField] private GameIntEvent gameEvent;

    public UnityEvent<int> response;

    private void OnEnable()
    {
        gameEvent.Register(this);
    }

    private void OnDisable()
    {
        gameEvent.Unregister(this);
    }

    public void OnEventRaised(int value)
    {
        response?.Invoke(value);
    }
}