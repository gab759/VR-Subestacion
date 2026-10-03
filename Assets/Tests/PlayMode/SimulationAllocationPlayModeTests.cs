using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using VRSubestacion.Simulation.Integration;

namespace VRSubestacion.Simulation.PlayModeTests
{
    /// <summary>
    /// Presupuesto de GC para 90 Hz en Quest 3: tras el warmup, ningún scope del bucle de simulación
    /// (Start / OnSignal / Tick, incluyendo la propagación al presenter y a TMP) asigna en heap.
    /// El código del test y del runner queda fuera de los scopes medidos.
    /// </summary>
    public sealed class SimulationAllocationPlayModeTests
    {
        private const int WarmupCycles = 2;
        private const int MeasuredCycles = 3;
        private const int IdleFrames = 30;
        private const int FramesPerCycleGuard = 120;

        private SimulationRig _rig;

        [SetUp]
        public void SetUp()
        {
            if (!SimulationAllocationProbe.IsSupported)
                Assert.Ignore("SimulationAllocationProbe solo mide en el Editor.");

            SimulationAllocationProbe.Enabled = false;
            SimulationAllocationProbe.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            SimulationAllocationProbe.Enabled = false;
            SimulationAllocationProbe.Reset();
            _rig?.Destroy();
            _rig = null;
        }

        [UnityTest]
        public IEnumerator SimulationLoop_AfterWarmup_HasZeroGCAlloc()
        {
            _rig = SimulationRig.Create();
            _rig.Activate();
            yield return null;

            for (int i = 0; i < WarmupCycles; i++)
                yield return RunCycle();

            SimulationAllocationProbe.Reset();
            SimulationAllocationProbe.Enabled = true;

            for (int i = 0; i < MeasuredCycles; i++)
                yield return RunCycle();

            for (int i = 0; i < IdleFrames; i++)
                yield return null;

            SimulationAllocationProbe.Enabled = false;

            int minimumSamples = MeasuredCycles * (_rig.StepCount + 1) + IdleFrames;
            Assert.GreaterOrEqual(SimulationAllocationProbe.SampleCount, minimumSamples,
                "Los scopes de Start/OnSignal/Tick deben haberse medido.");
            Assert.AreEqual(0, SimulationAllocationProbe.ViolationCount,
                $"GC.Alloc en el bucle de simulación: {SimulationAllocationProbe.AllocatedBytes} B en " +
                $"{SimulationAllocationProbe.ViolationCount} scopes (máx. {SimulationAllocationProbe.MaxScopeBytes} B).");
        }

        [UnityTest]
        public IEnumerator AllocationMonitor_MeasuresAfterWarmupAndReportsCleanLoop()
        {
            _rig = SimulationRig.Create();
            var monitor = _rig.Root.AddComponent<SimulationAllocationMonitor>();
            monitor.Configure(warmup: 45, frameBudgetBytes: 0, logViolations: true);
            _rig.Activate();
            yield return null;

            int guard = 0;
            while (!monitor.IsMeasuring && guard++ < 10)
                yield return RunCycle();

            Assert.IsTrue(monitor.IsMeasuring, "El monitor debe salir del warmup.");
            Assert.IsTrue(SimulationAllocationProbe.Enabled);

            for (int i = 0; i < MeasuredCycles; i++)
                yield return RunCycle();

            Assert.Greater(monitor.MeasuredFrames, 0);
            Assert.Greater(monitor.ScopedSamples, 0);
            Assert.AreEqual(0, monitor.ScopedViolations, $"{monitor.ScopedAllocatedBytes} B asignados en el bucle de simulación.");

            monitor.enabled = false;
            Assert.IsFalse(SimulationAllocationProbe.Enabled, "Al deshabilitarse, el monitor apaga la sonda.");
        }

        private IEnumerator RunCycle()
        {
            _rig.Context.RestartProcedure();
            _rig.Emitter.Play(_rig.Input, SubstationProcedureFactory.OrderedSignalIds);

            int guard = 0;
            while (!_rig.Emitter.IsDone && guard++ < FramesPerCycleGuard)
                yield return null;

            yield return null;
            Assert.IsTrue(_rig.Director.IsComplete, "El ciclo debe completar el procedimiento.");
        }
    }
}
