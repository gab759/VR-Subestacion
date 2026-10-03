using UnityEngine;

namespace VRSubestacion.Simulation
{
    /// <summary>
    /// Canal ScriptableObject. Raise recorre un array preasignado; no hay List ni multicast Action.
    /// </summary>
    [CreateAssetMenu(
        fileName = "InteractionSignalChannel",
        menuName = "VR Subestacion/Interaction Signal Channel")]
    public sealed class InteractionSignalChannel : ScriptableObject
    {
        private const int DefaultCapacity = 8;

        private IInteractionSignalListener[] _listeners;
        private int _count;

        private void OnEnable()
        {
            EnsureBuffer();
            _count = 0;
        }

        private void OnDisable()
        {
            _count = 0;
        }

        public int ListenerCount => _count;

        public void Subscribe(IInteractionSignalListener listener)
        {
            if (listener == null)
                return;

            EnsureBuffer();

            for (int i = 0; i < _count; i++)
            {
                if (ReferenceEquals(_listeners[i], listener))
                    return;
            }

            if (_count == _listeners.Length)
                Grow();

            _listeners[_count] = listener;
            _count++;
        }

        public void Unsubscribe(IInteractionSignalListener listener)
        {
            if (listener == null || _listeners == null)
                return;

            for (int i = 0; i < _count; i++)
            {
                if (!ReferenceEquals(_listeners[i], listener))
                    continue;

                int last = _count - 1;
                _listeners[i] = _listeners[last];
                _listeners[last] = null;
                _count = last;
                return;
            }
        }

        public void Raise(int signalId, int payload = 0)
        {
            var signal = new InteractionSignal(signalId, payload);
            Raise(in signal);
        }

        public void Raise(in InteractionSignal signal)
        {
            if (_listeners == null)
                return;

            for (int i = 0; i < _count; i++)
                _listeners[i].OnSignal(in signal);
        }

        private void EnsureBuffer()
        {
            if (_listeners == null)
                _listeners = new IInteractionSignalListener[DefaultCapacity];
        }

        private void Grow()
        {
            var grown = new IInteractionSignalListener[_listeners.Length * 2];
            for (int i = 0; i < _count; i++)
                grown[i] = _listeners[i];
            _listeners = grown;
        }
    }
}
