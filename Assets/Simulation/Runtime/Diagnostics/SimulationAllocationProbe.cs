using System;
using UnityEngine;

namespace VRSubestacion.Simulation
{
    /// <summary>
    /// Mide bytes de heap asignados dentro de los scopes del bucle de simulación
    /// (<see cref="SimulationSampleScope"/>). Solo cuenta el scope más externo, así que
    /// director → canal de progreso → presenter → TMP se mide como una sola muestra.
    /// Apagada no lee el GC; en players (IL2CPP) no está soportada y queda como no-op.
    /// </summary>
    public static class SimulationAllocationProbe
    {
#if UNITY_EDITOR
        public static bool IsSupported => true;
#else
        public static bool IsSupported => false;
#endif

        private static bool _enabled;
        private static bool _measuring;
        private static int _depth;
        private static long _scopeStart;
        private static long _allocatedBytes;
        private static long _maxScopeBytes;
        private static int _samples;
        private static int _violations;

        public static bool Enabled
        {
            get => _enabled;
            set => _enabled = value && IsSupported;
        }

        public static long AllocatedBytes => _allocatedBytes;
        public static long MaxScopeBytes => _maxScopeBytes;
        public static int SampleCount => _samples;
        public static int ViolationCount => _violations;

        public static void Reset()
        {
            _allocatedBytes = 0;
            _maxScopeBytes = 0;
            _samples = 0;
            _violations = 0;
        }

        public static void Begin()
        {
            if (_depth++ != 0 || !_enabled)
                return;

            _measuring = true;
            _scopeStart = ReadAllocatedBytes();
        }

        public static void End()
        {
            if (_depth == 0 || --_depth != 0 || !_measuring)
                return;

            _measuring = false;
            long delta = ReadAllocatedBytes() - _scopeStart;
            _samples++;

            if (delta <= 0)
                return;

            _violations++;
            _allocatedBytes += delta;
            if (delta > _maxScopeBytes)
                _maxScopeBytes = delta;
        }

        private static long ReadAllocatedBytes()
        {
#if UNITY_EDITOR
            return GC.GetAllocatedBytesForCurrentThread();
#else
            return 0;
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _enabled = false;
            _measuring = false;
            _depth = 0;
            Reset();
        }
    }
}
