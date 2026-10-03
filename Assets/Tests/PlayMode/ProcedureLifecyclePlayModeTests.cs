using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VRSubestacion.Simulation.Integration;

namespace VRSubestacion.Simulation.PlayModeTests
{
    /// <summary>
    /// Ciclo de vida completo en Play Mode: Awake/OnEnable enlazan, Start arranca, las señales
    /// avanzan el director frame a frame y el presenter refleja cada transición en las vistas TMP.
    /// </summary>
    public sealed class ProcedureLifecyclePlayModeTests
    {
        private sealed class ProgressSpy : IProcedureProgressListener
        {
            public readonly ProcedureProgressKind[] Kinds = new ProcedureProgressKind[64];
            public readonly ProcedureStepState[] States = new ProcedureStepState[64];
            public readonly int[] StepIds = new int[64];
            public int Count;

            public void OnProgress(in ProcedureProgress progress)
            {
                if (Count < Kinds.Length)
                {
                    Kinds[Count] = progress.Kind;
                    States[Count] = progress.State;
                    StepIds[Count] = progress.StepId;
                }
                Count++;
            }
        }

        private SimulationRig _rig;

        [TearDown]
        public void TearDown()
        {
            _rig?.Destroy();
            _rig = null;
        }

        [UnityTest]
        public IEnumerator Activation_BindsInAwakeAndStartsOnFirstFrame()
        {
            _rig = SimulationRig.Create();
            _rig.Activate();

            Assert.AreEqual(SimulationLifecycle.Bound, _rig.Binder.Lifecycle, "OnEnable enlaza, Start todavía no corrió.");
            Assert.AreEqual(1, _rig.Input.ListenerCount, "Solo el director escucha el canal de entrada.");
            Assert.AreEqual(1, _rig.Progress.ListenerCount, "Solo el presenter escucha el canal de progreso.");
            Assert.AreSame(_rig.Progress, _rig.Presenter.ProgressChannel, "El contexto inyecta el canal al presenter.");

            yield return null;

            Assert.AreEqual(SimulationLifecycle.Running, _rig.Binder.Lifecycle);
            Assert.AreEqual(0, _rig.Director.CurrentStepIndex);
            AssertUi(completed: 0, activeRow: 0);
        }

        [UnityTest]
        public IEnumerator FullProcedure_SignalsInOrder_AdvanceEveryStepAndReachUi()
        {
            _rig = SimulationRig.Create();
            var spy = new ProgressSpy();
            _rig.Progress.Subscribe(spy);
            _rig.Activate();
            yield return null;

            int[] signals = SubstationProcedureFactory.OrderedSignalIds;
            Assert.AreEqual(_rig.StepCount, signals.Length);

            for (int i = 0; i < signals.Length; i++)
            {
                Assert.AreEqual(signals[i], _rig.Director.CurrentExpectedSignalId, $"Señal esperada en el paso {i}.");

                _rig.Input.Raise(signals[i]);
                yield return null;

                Assert.AreEqual(i + 1, _rig.Director.CurrentStepIndex);
                AssertUi(completed: i + 1, activeRow: i + 1 < signals.Length ? i + 1 : -1);
            }

            Assert.IsTrue(_rig.Director.IsComplete);
            Assert.IsTrue(_rig.Presenter.Core.IsProcedureComplete);
            Assert.AreEqual(SimulationRig.CompletedText, _rig.ActiveStepLabel.text);

            AssertProgressSequence(spy);
            _rig.Progress.Unsubscribe(spy);
        }

        [UnityTest]
        public IEnumerator OutOfOrderOrRepeatedSignals_AreIgnored()
        {
            _rig = SimulationRig.Create();
            _rig.Activate();
            yield return null;

            int[] signals = SubstationProcedureFactory.OrderedSignalIds;

            _rig.Input.Raise(signals[1]);
            _rig.Input.Raise(signals[5]);
            _rig.Input.Raise(InteractionSignalIds.None);
            _rig.Input.Raise(999);
            yield return null;

            Assert.AreEqual(0, _rig.Director.CurrentStepIndex);
            AssertUi(completed: 0, activeRow: 0);

            _rig.Input.Raise(signals[0]);
            _rig.Input.Raise(signals[0]);
            yield return null;

            Assert.AreEqual(1, _rig.Director.CurrentStepIndex, "Repetir la señal del paso completado no avanza el siguiente.");
            AssertUi(completed: 1, activeRow: 1);
        }

        [UnityTest]
        public IEnumerator SignalsFromUpdate_DriveProcedureToCompletion()
        {
            _rig = SimulationRig.Create();
            _rig.Activate();
            yield return null;

            _rig.Emitter.Play(_rig.Input, SubstationProcedureFactory.OrderedSignalIds);

            int guard = 0;
            while (!_rig.Emitter.IsDone && guard++ < 120)
                yield return null;

            Assert.IsTrue(_rig.Emitter.IsDone);
            Assert.IsTrue(_rig.Director.IsComplete);
            AssertUi(completed: _rig.StepCount, activeRow: -1);
        }

        [UnityTest]
        public IEnumerator DisablingContext_UnbindsAndReEnabling_ResumesAtSameStep()
        {
            _rig = SimulationRig.Create();
            _rig.Activate();
            yield return null;

            int[] signals = SubstationProcedureFactory.OrderedSignalIds;
            for (int i = 0; i < 3; i++)
                _rig.Input.Raise(signals[i]);
            yield return null;

            _rig.Context.enabled = false;
            yield return null;

            Assert.AreEqual(SimulationLifecycle.Unbound, _rig.Binder.Lifecycle);
            Assert.AreEqual(0, _rig.Input.ListenerCount);

            _rig.Input.Raise(signals[3]);
            yield return null;
            Assert.AreEqual(3, _rig.Director.CurrentStepIndex, "Desenlazado no recibe señales.");

            _rig.Context.enabled = true;
            yield return null;

            Assert.AreEqual(SimulationLifecycle.Running, _rig.Binder.Lifecycle, "Start no vuelve a correr; el procedimiento sigue en curso.");
            _rig.Input.Raise(signals[3]);
            yield return null;

            Assert.AreEqual(4, _rig.Director.CurrentStepIndex);
            AssertUi(completed: 4, activeRow: 4);
        }

        [UnityTest]
        public IEnumerator RestartProcedure_ResetsDirectorAndUi()
        {
            _rig = SimulationRig.Create();
            _rig.Activate();
            yield return null;

            int[] signals = SubstationProcedureFactory.OrderedSignalIds;
            for (int i = 0; i < signals.Length; i++)
                _rig.Input.Raise(signals[i]);
            yield return null;
            Assert.IsTrue(_rig.Director.IsComplete);

            _rig.Context.RestartProcedure();
            yield return null;

            Assert.AreEqual(0, _rig.Director.CurrentStepIndex);
            Assert.IsFalse(_rig.Presenter.Core.IsProcedureComplete);
            AssertUi(completed: 0, activeRow: 0);
        }

#if !VRSUB_FORCE_LEGACY && !VRSUB_FORCE_CLEAN
        [UnityTest]
        public IEnumerator ShadowBridge_Legacy_KeepsNewArchitectureDormant()
        {
            _rig = SimulationRig.CreateWithBridge(ArchitectureMode.Legacy);
            _rig.Activate();
            yield return null;

            Assert.IsTrue(_rig.LegacyBehaviour.enabled);
            Assert.IsTrue(_rig.LegacyPanel.activeSelf);
            Assert.IsFalse(_rig.Context.enabled);
            Assert.IsFalse(_rig.ChecklistCanvas.activeSelf);
            Assert.AreEqual(0, _rig.Input.ListenerCount);
            Assert.AreEqual(0, _rig.Progress.ListenerCount);

            _rig.Input.Raise(SubstationProcedureFactory.OrderedSignalIds[0]);
            yield return null;

            Assert.IsTrue(_rig.Binder == null || !_rig.Director.IsStarted, "En Legacy el director nunca arranca.");
        }

        [UnityTest]
        public IEnumerator ShadowBridge_SwitchingAtRuntime_StartsPausesAndResumesNewFlow()
        {
            _rig = SimulationRig.CreateWithBridge(ArchitectureMode.Legacy);
            _rig.Activate();
            yield return null;

            _rig.Bridge.SetMode(ArchitectureMode.Shadow);
            yield return null;

            Assert.IsTrue(_rig.LegacyBehaviour.enabled, "Shadow mantiene el flujo anterior.");
            Assert.IsTrue(_rig.LegacyPanel.activeSelf);
            Assert.AreEqual(SimulationLifecycle.Running, _rig.Binder.Lifecycle, "Start corre al habilitarse por primera vez.");
            AssertUi(completed: 0, activeRow: 0);

            int[] signals = SubstationProcedureFactory.OrderedSignalIds;
            _rig.Input.Raise(signals[0]);
            yield return null;
            AssertUi(completed: 1, activeRow: 1);

            _rig.Bridge.SetMode(ArchitectureMode.Legacy);
            yield return null;

            Assert.AreEqual(SimulationLifecycle.Unbound, _rig.Binder.Lifecycle);
            _rig.Input.Raise(signals[1]);
            yield return null;
            Assert.AreEqual(1, _rig.Director.CurrentStepIndex);

            _rig.Bridge.SetMode(ArchitectureMode.Clean);
            yield return null;

            Assert.IsFalse(_rig.LegacyBehaviour.enabled);
            Assert.IsFalse(_rig.LegacyPanel.activeSelf);
            Assert.AreEqual(SimulationLifecycle.Running, _rig.Binder.Lifecycle);

            _rig.Input.Raise(signals[1]);
            yield return null;
            Assert.AreEqual(2, _rig.Director.CurrentStepIndex);
            AssertUi(completed: 2, activeRow: 2);
        }
#endif

        private void AssertUi(int completed, int activeRow)
        {
            for (int i = 0; i < _rig.StepCount; i++)
            {
                ProcedureStepState expected = i < completed
                    ? ProcedureStepState.Completed
                    : (i == activeRow ? ProcedureStepState.Active : ProcedureStepState.Pending);

                Assert.AreEqual(expected, _rig.Presenter.Core.GetState(i), $"Estado del core en la fila {i}.");
                Assert.AreEqual(expected, _rig.Views[i].State, $"Estado de la vista en la fila {i}.");
                Assert.AreEqual(_rig.Style.Resolve(expected).Color, LabelColor(i), $"Color de la fila {i}.");
            }

            Assert.AreEqual(activeRow, _rig.Presenter.Core.ActiveRow);
            Assert.AreEqual($"{completed}/{_rig.StepCount}", _rig.ProgressLabel.text);

            if (activeRow >= 0)
                Assert.AreEqual(_rig.Titles[activeRow], _rig.ActiveStepLabel.text);
        }

        private Color LabelColor(int row)
        {
            return _rig.Views[row].GetComponent<TMPro.TMP_Text>().color;
        }

        private void AssertProgressSequence(ProgressSpy spy)
        {
            int steps = _rig.StepCount;
            Assert.AreEqual(2 + steps * 2, spy.Count, "Started + Active(0) + (Completed, Active|ProcedureCompleted) por paso.");

            Assert.AreEqual(ProcedureProgressKind.ProcedureStarted, spy.Kinds[0]);
            AssertStepEvent(spy, 1, _rig.StepIds[0], ProcedureStepState.Active);

            int e = 2;
            for (int i = 0; i < steps; i++)
            {
                AssertStepEvent(spy, e++, _rig.StepIds[i], ProcedureStepState.Completed);

                if (i + 1 < steps)
                    AssertStepEvent(spy, e++, _rig.StepIds[i + 1], ProcedureStepState.Active);
                else
                    Assert.AreEqual(ProcedureProgressKind.ProcedureCompleted, spy.Kinds[e++]);
            }
        }

        private static void AssertStepEvent(ProgressSpy spy, int index, int stepId, ProcedureStepState state)
        {
            Assert.AreEqual(ProcedureProgressKind.StepStateChanged, spy.Kinds[index], $"Evento {index}.");
            Assert.AreEqual(stepId, spy.StepIds[index], $"StepId del evento {index}.");
            Assert.AreEqual(state, spy.States[index], $"Estado del evento {index}.");
        }
    }
}
