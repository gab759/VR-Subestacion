using UnityEngine;

namespace VRSubestacion.Simulation
{
    /// <summary>
    /// Canal ScriptableObject de salida (director hacia vistas). Raise recorre un array preasignado.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ProcedureProgressChannel",
        menuName = "VR Subestacion/Procedure Progress Channel")]
    public sealed class ProcedureProgressChannel : ScriptableObject
    {
        private const int DefaultCapacity = 4;

        private IProcedureProgressListener[] _listeners;
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

        public void Subscribe(IProcedureProgressListener listener)
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

        public void Unsubscribe(IProcedureProgressListener listener)
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

        public void Raise(in ProcedureProgress progress)
        {
            if (_listeners == null)
                return;

            for (int i = 0; i < _count; i++)
                _listeners[i].OnProgress(in progress);
        }

        private void EnsureBuffer()
        {
            if (_listeners == null)
                _listeners = new IProcedureProgressListener[DefaultCapacity];
        }

        private void Grow()
        {
            var grown = new IProcedureProgressListener[_listeners.Length * 2];
            for (int i = 0; i < _count; i++)
                grown[i] = _listeners[i];
            _listeners = grown;
        }
    }
}
