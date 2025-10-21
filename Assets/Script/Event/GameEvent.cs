using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.Scripts.GameEvents
{
    [CreateAssetMenu(fileName = "Void Event", menuName = "Game Events/Void Event")]
    public class GameEvent : ScriptableObject
    {
        private List<GameEventListener> listeners;

        private void OnEnable()
        {
            listeners = new List<GameEventListener>();
        }

        private void OnDisable()
        {
            listeners.Clear();
        }

        public void SetUp()
        {
            listeners = new List<GameEventListener>();
        }

        public void Raise()
        {
            foreach (GameEventListener sub in listeners)
            {
                sub.OnEventRaised();
            }
        }

        public void Register(GameEventListener newListener)
        {
            if (listeners.Contains(newListener)) return;

            listeners.Add(newListener);
        }

        public void Unregister(GameEventListener newListener)
        {
            if (!listeners.Contains(newListener)) return;

            listeners.Remove(newListener);
        }
    }
}