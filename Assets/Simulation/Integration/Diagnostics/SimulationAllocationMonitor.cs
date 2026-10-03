using Unity.Profiling;
using UnityEngine;

namespace VRSubestacion.Simulation.Integration
{
    /// <summary>
    /// Diagnóstico de GC para 90 Hz. Tras <c>warmupFrames</c> activa <see cref="SimulationAllocationProbe"/>
    /// (asignaciones dentro de Tick / OnSignal / Start del director, solo Editor) y lee el contador
    /// "GC Allocated In Frame" del Profiler (frame completo; Editor y development builds).
    /// Solo escribe en consola al detectar una violación o al deshabilitarse.
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrder)]
    [DisallowMultipleComponent]
    [AddComponentMenu("VR Subestacion/Diagnostics/Simulation Allocation Monitor")]
    public sealed class SimulationAllocationMonitor : MonoBehaviour
    {
        public const int ExecutionOrder = 10000;
        public const string FrameAllocCounterName = "GC Allocated In Frame";

        [Tooltip("Frames ignorados al habilitar (carga de escena, primer layout de UI, JIT). 180 = 2 s a 90 Hz.")]
        [SerializeField, Min(0)] private int warmupFrames = 180;

        [Tooltip("Error en consola por cada frame en que el bucle de simulación asignó heap.")]
        [SerializeField] private bool logScopedViolations = true;

        [Tooltip("Lee el contador de frame completo (incluye Meta XR SDK, UI y el resto de scripts).")]
        [SerializeField] private bool trackFrameAllocations = true;

        [Tooltip("Bytes por frame tolerados en el contador de frame completo antes de contar el frame como excedido.")]
        [SerializeField, Min(0)] private int frameAllocationBudgetBytes;

        [SerializeField] private bool logSummaryOnDisable = true;

        private ProfilerRecorder _frameRecorder;
        private int _warmupRemaining;
        private bool _measuring;
        private int _reportedViolations;
        private int _measuredFrames;
        private int _framesOverBudget;
        private long _maxFrameBytes;

        public bool IsMeasuring => _measuring;
        public bool IsFrameCounterAvailable => _frameRecorder.Valid;
        public int MeasuredFrames => _measuredFrames;
        public int FramesOverBudget => _framesOverBudget;
        public long MaxFrameAllocatedBytes => _maxFrameBytes;
        public int ScopedSamples => SimulationAllocationProbe.SampleCount;
        public int ScopedViolations => SimulationAllocationProbe.ViolationCount;
        public long ScopedAllocatedBytes => SimulationAllocationProbe.AllocatedBytes;

        public void Configure(int warmup, int frameBudgetBytes, bool logViolations)
        {
            warmupFrames = warmup < 0 ? 0 : warmup;
            frameAllocationBudgetBytes = frameBudgetBytes < 0 ? 0 : frameBudgetBytes;
            logScopedViolations = logViolations;
        }

        private void OnEnable()
        {
            _warmupRemaining = warmupFrames;
            _measuring = false;
            _reportedViolations = 0;
            _measuredFrames = 0;
            _framesOverBudget = 0;
            _maxFrameBytes = 0;

            if (trackFrameAllocations)
                _frameRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, FrameAllocCounterName);
        }

        private void LateUpdate()
        {
            if (!_measuring)
            {
                if (_warmupRemaining > 0)
                {
                    _warmupRemaining--;
                    return;
                }

                SimulationAllocationProbe.Reset();
                SimulationAllocationProbe.Enabled = true;
                _measuring = true;
                return;
            }

            _measuredFrames++;

            if (_frameRecorder.Valid)
            {
                long frameBytes = _frameRecorder.LastValue;
                if (frameBytes > _maxFrameBytes)
                    _maxFrameBytes = frameBytes;
                if (frameBytes > frameAllocationBudgetBytes)
                    _framesOverBudget++;
            }

            int violations = SimulationAllocationProbe.ViolationCount;
            if (violations == _reportedViolations)
                return;

            _reportedViolations = violations;
            if (logScopedViolations)
            {
                Debug.LogError(
                    $"[SimulationAllocationMonitor] GC.Alloc en el bucle de simulación (frame {Time.frameCount}): " +
                    $"{SimulationAllocationProbe.AllocatedBytes} B acumulados, máx. {SimulationAllocationProbe.MaxScopeBytes} B por scope. " +
                    $"Activa Deep Profile y filtra por '{SimulationProfilerMarkers.SignalName}' / '{SimulationProfilerMarkers.TickName}'.",
                    this);
            }
        }

        private void OnDisable()
        {
            if (_measuring)
            {
                SimulationAllocationProbe.Enabled = false;
                if (logSummaryOnDisable)
                    LogSummary();
            }

            _measuring = false;

            if (_frameRecorder.Valid)
                _frameRecorder.Dispose();
        }

        private void LogSummary()
        {
            string scoped = SimulationAllocationProbe.IsSupported
                ? $"bucle de simulación: {SimulationAllocationProbe.SampleCount} muestras, {SimulationAllocationProbe.ViolationCount} con GC.Alloc ({SimulationAllocationProbe.AllocatedBytes} B)"
                : "bucle de simulación: sonda no soportada en este player (usa el Profiler)";
            string frame = _frameRecorder.Valid
                ? $"frame completo: {_framesOverBudget}/{_measuredFrames} frames sobre {frameAllocationBudgetBytes} B, máx. {_maxFrameBytes} B"
                : "frame completo: contador no disponible (requiere Editor o Development Build)";
            string message = $"[SimulationAllocationMonitor] {scoped}; {frame}.";

            if (SimulationAllocationProbe.ViolationCount > 0)
                Debug.LogError(message, this);
            else
                Debug.Log(message, this);
        }
    }
}
