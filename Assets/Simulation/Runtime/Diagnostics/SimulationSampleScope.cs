using System;
using Unity.Profiling;

namespace VRSubestacion.Simulation
{
    /// <summary>
    /// Marcadores visibles en el Profiler (y en Deep Profile) para filtrar el bucle de simulación.
    /// </summary>
    public static class SimulationProfilerMarkers
    {
        public const string TickName = "VRSub.Simulation.Tick";
        public const string SignalName = "VRSub.Simulation.OnSignal";
        public const string StartName = "VRSub.Simulation.Start";

        public static readonly ProfilerMarker Tick = new ProfilerMarker(ProfilerCategory.Scripts, TickName);
        public static readonly ProfilerMarker Signal = new ProfilerMarker(ProfilerCategory.Scripts, SignalName);
        public static readonly ProfilerMarker Start = new ProfilerMarker(ProfilerCategory.Scripts, StartName);
    }

    /// <summary>
    /// Struct para <c>using</c>: abre el marcador del Profiler y la muestra de
    /// <see cref="SimulationAllocationProbe"/>. No hay boxing al ser un struct local.
    /// </summary>
    public readonly struct SimulationSampleScope : IDisposable
    {
        private readonly ProfilerMarker _marker;

        public SimulationSampleScope(ProfilerMarker marker)
        {
            _marker = marker;
            _marker.Begin();
            SimulationAllocationProbe.Begin();
        }

        public void Dispose()
        {
            SimulationAllocationProbe.End();
            _marker.End();
        }
    }
}
