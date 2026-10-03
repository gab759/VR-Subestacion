using System;
using NUnit.Framework;
using UnityEngine;
using VRSubestacion.Presentation;

namespace VRSubestacion.Simulation.Tests
{
    public sealed class SimulationBinderTests
    {
        private sealed class CountingView : IChecklistItemView
        {
            public ProcedureStepState State;

            public void SetState(ProcedureStepState state)
            {
                State = state;
            }
        }

        private InteractionSignalChannel _input;
        private ProcedureProgressChannel _progress;
        private SimulationBinder _binder;

        [SetUp]
        public void SetUp()
        {
            _input = ScriptableObject.CreateInstance<InteractionSignalChannel>();
            _progress = ScriptableObject.CreateInstance<ProcedureProgressChannel>();
            _binder = SimulationBinder.CreateSubstation();
        }

        [TearDown]
        public void TearDown()
        {
            _binder?.Unbind();
            UnityEngine.Object.DestroyImmediate(_input);
            UnityEngine.Object.DestroyImmediate(_progress);
        }

        [Test]
        public void Constructor_RejectsNullDirector()
        {
            Assert.Throws<ArgumentNullException>(() => new SimulationBinder(null));
        }

        [Test]
        public void NewBinder_IsUnbound()
        {
            Assert.AreEqual(SimulationLifecycle.Unbound, _binder.Lifecycle);
            Assert.IsFalse(_binder.IsBound);
            Assert.IsNull(_binder.InputChannel);
            Assert.IsNull(_binder.ProgressChannel);
        }

        [Test]
        public void Bind_WithBothChannels_SubscribesDirectorWithoutIssues()
        {
            SimulationBindingIssues issues = _binder.Bind(_input, _progress);

            Assert.AreEqual(SimulationBindingIssues.None, issues);
            Assert.AreEqual(SimulationLifecycle.Bound, _binder.Lifecycle);
            Assert.AreEqual(1, _input.ListenerCount);
            Assert.AreSame(_input, _binder.InputChannel);
            Assert.AreSame(_progress, _binder.ProgressChannel);
        }

        [Test]
        public void Bind_WithoutInput_ReportsIssueAndStaysUnbound()
        {
            SimulationBindingIssues issues = _binder.Bind(null, _progress);

            Assert.IsTrue((issues & SimulationBindingIssues.MissingInputChannel) != 0);
            Assert.IsFalse(_binder.IsBound);
            Assert.IsFalse(_binder.StartProcedure());
        }

        [Test]
        public void Bind_WithoutProgress_BindsButReportsIssue()
        {
            SimulationBindingIssues issues = _binder.Bind(_input, null);

            Assert.AreEqual(SimulationBindingIssues.MissingProgressChannel, issues);
            Assert.IsTrue(_binder.IsBound);
            Assert.IsTrue(_binder.StartProcedure());

            _input.Raise(InteractionSignalIds.BajarSwitchPrincipal);
            Assert.AreEqual(1, _binder.Director.CurrentStepIndex);
        }

        [Test]
        public void StartProcedure_BeforeBind_ReturnsFalse()
        {
            Assert.IsFalse(_binder.StartProcedure());
            Assert.IsFalse(_binder.Director.IsStarted);
        }

        [Test]
        public void Lifecycle_BindStartUnbindRebind_PreservesProcedureProgress()
        {
            _binder.Bind(_input, _progress);
            Assert.IsTrue(_binder.StartProcedure());
            Assert.AreEqual(SimulationLifecycle.Running, _binder.Lifecycle);

            _input.Raise(InteractionSignalIds.BajarSwitchPrincipal);
            Assert.AreEqual(1, _binder.Director.CurrentStepIndex);

            _binder.Unbind();
            Assert.AreEqual(SimulationLifecycle.Unbound, _binder.Lifecycle);
            Assert.AreEqual(0, _input.ListenerCount);

            _input.Raise(InteractionSignalIds.BajarProtectores);
            Assert.AreEqual(1, _binder.Director.CurrentStepIndex, "Desenlazado no debe avanzar.");

            _binder.Bind(_input, _progress);
            Assert.AreEqual(SimulationLifecycle.Running, _binder.Lifecycle);

            _input.Raise(InteractionSignalIds.BajarProtectores);
            Assert.AreEqual(2, _binder.Director.CurrentStepIndex);
        }

        [Test]
        public void Rebind_ToDifferentChannel_ReleasesPreviousChannel()
        {
            var other = ScriptableObject.CreateInstance<InteractionSignalChannel>();
            try
            {
                _binder.Bind(_input, _progress);
                _binder.Bind(other, _progress);

                Assert.AreEqual(0, _input.ListenerCount);
                Assert.AreEqual(1, other.ListenerCount);
            }
            finally
            {
                _binder.Unbind();
                UnityEngine.Object.DestroyImmediate(other);
            }
        }

        [Test]
        public void Unbind_StopsProgressPublishing()
        {
            var view = new CountingView();
            var presenter = new ChecklistPresenterCore(
                new[] { ProcedureStepIds.BajarSwitchPrincipal },
                new IChecklistItemView[] { view },
                null);
            presenter.Bind(_progress);

            _binder.Bind(_input, _progress);
            _binder.Unbind();
            _binder.Director.StartProcedure();

            Assert.AreEqual(ProcedureStepState.Pending, view.State);
            presenter.Unbind();
        }

        [Test]
        public void BoundBinder_DrivesPresenterThroughProgressChannel()
        {
            var view = new CountingView();
            var presenter = new ChecklistPresenterCore(
                new[] { ProcedureStepIds.BajarSwitchPrincipal },
                new IChecklistItemView[] { view },
                null);
            presenter.Bind(_progress);

            _binder.Bind(_input, _progress);
            _binder.StartProcedure();
            Assert.AreEqual(ProcedureStepState.Active, view.State);

            _input.Raise(InteractionSignalIds.BajarSwitchPrincipal);
            Assert.AreEqual(ProcedureStepState.Completed, view.State);

            presenter.Unbind();
        }

        [Test]
        public void TickAndFullCycle_DoNotAllocateAfterWarmup()
        {
            var views = new IChecklistItemView[SubstationProcedureFactory.StepCount];
            for (int i = 0; i < views.Length; i++)
                views[i] = new CountingView();
            var presenter = new ChecklistPresenterCore(SubstationProcedureFactory.OrderedSignalIds, views, null);
            presenter.Bind(_progress);

            _binder.Bind(_input, _progress);
            RunCycle();

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int cycle = 0; cycle < 4; cycle++)
            {
                RunCycle();
                _binder.Unbind();
                _binder.Bind(_input, _progress);
            }
            long after = GC.GetAllocatedBytesForCurrentThread();

            Assert.AreEqual(before, after, "Bind/Start/Tick/Raise/Unbind no deben asignar en heap tras el warmup.");
            presenter.Unbind();
        }

        private void RunCycle()
        {
            _binder.StartProcedure();
            int[] ids = SubstationProcedureFactory.OrderedSignalIds;
            for (int i = 0; i < ids.Length; i++)
            {
                _binder.Tick(0.016f);
                _input.Raise(ids[i]);
            }
        }
    }
}
