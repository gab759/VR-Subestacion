using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.GameEvents;
using UnityEngine;

[CreateAssetMenu(fileName = "Int Event", menuName = "Game Events/Int Event")]
public class GameIntEvent : ScriptableObject
{
    private List<GameIntEventListener> listeners = new List<GameIntEventListener>();

    private void OnEnable()
    {
        listeners = new List<GameIntEventListener>();
    }

    private void OnDisable()
    {
        listeners.Clear();
    }

    public void SetUp()
    {
        listeners = new List<GameIntEventListener>();
    }

    public void Raise(int value)
    {
        for (int i = listeners.Count - 1; i >= 0; i--)
        {
            listeners[i].OnEventRaised(value);
        }
    }

    public void Register(GameIntEventListener newListener)
    {
        if (listeners.Contains(newListener)) return;

        listeners.Add(newListener);
    }

    public void Unregister(GameIntEventListener newListener)
    {
        if (!listeners.Contains(newListener)) return;

        listeners.Remove(newListener);
    }
}