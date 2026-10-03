using UnityEngine;

namespace VRSubestacion.Simulation.PlayModeTests
{
    /// <summary>
    /// Sustituto de un adaptador Meta XR: emite señales desde Update, como lo haría un callback de interacción.
    /// </summary>
    public sealed class ScriptedSignalEmitter : MonoBehaviour
    {
        private InteractionSignalChannel _channel;
        private int[] _sequence;
        private int _next;
        private int _signalsPerFrame = 1;

        public bool IsDone => _sequence == null || _next >= _sequence.Length;
        public int Emitted => _next;

        public void Play(InteractionSignalChannel channel, int[] sequence, int signalsPerFrame = 1)
        {
            _channel = channel;
            _sequence = sequence;
            _next = 0;
            _signalsPerFrame = signalsPerFrame < 1 ? 1 : signalsPerFrame;
        }

        private void Update()
        {
            if (_channel == null || IsDone)
                return;

            for (int i = 0; i < _signalsPerFrame && !IsDone; i++)
                _channel.Raise(_sequence[_next++]);
        }
    }
}
