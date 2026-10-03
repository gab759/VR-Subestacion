using System;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRSubestacion.Presentation;

namespace VRSubestacion.Simulation.Tests
{
    public sealed class ChecklistPresenterTests
    {
        private sealed class FakeItemView : IChecklistItemView
        {
            public ProcedureStepState State;
            public int SetStateCalls;

            public void SetState(ProcedureStepState state)
            {
                State = state;
                SetStateCalls++;
            }
        }

        private sealed class FakeProgressView : IChecklistProgressView
        {
            public int Completed;
            public int Total;
            public int ActiveRow = ChecklistPresenterCore.NoRow;
            public bool Complete;
            public int ProgressCalls;
            public int ActiveRowCalls;

            public void SetProgress(int completedCount, int stepCount)
            {
                Completed = completedCount;
                Total = stepCount;
                ProgressCalls++;
            }

            public void SetActiveRow(int row)
            {
                ActiveRow = row;
                ActiveRowCalls++;
            }

            public void SetProcedureComplete(bool complete)
            {
                Complete = complete;
            }
        }

        private InteractionSignalChannel _input;
        private ProcedureProgressChannel _progress;
        private ProceduralDirector _director;
        private FakeItemView[] _views;
        private FakeProgressView _progressView;
        private ChecklistPresenterCore _presenter;

        [SetUp]
        public void SetUp()
        {
            _input = ScriptableObject.CreateInstance<InteractionSignalChannel>();
            _progress = ScriptableObject.CreateInstance<ProcedureProgressChannel>();

            _director = ProceduralDirector.CreateSubstationProcedure();
            _director.Bind(_input);
            _director.BindProgress(_progress);

            int[] ids = SubstationProcedureFactory.OrderedSignalIds;
            _views = new FakeItemView[ids.Length];
            var views = new IChecklistItemView[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                _views[i] = new FakeItemView();
                views[i] = _views[i];
            }

            _progressView = new FakeProgressView();
            _presenter = new ChecklistPresenterCore(ids, views, _progressView);
            _presenter.Bind(_progress);
        }

        [TearDown]
        public void TearDown()
        {
            _presenter?.Unbind();
            _director?.Unbind();

            if (_input != null)
                UnityEngine.Object.DestroyImmediate(_input);
            if (_progress != null)
                UnityEngine.Object.DestroyImmediate(_progress);
        }

        [Test]
        public void Constructor_InitializesEveryRowAsPending()
        {
            for (int i = 0; i < _views.Length; i++)
            {
                Assert.AreEqual(ProcedureStepState.Pending, _presenter.GetState(i));
                Assert.AreEqual(ProcedureStepState.Pending, _views[i].State);
            }

            Assert.AreEqual(ChecklistPresenterCore.NoRow, _presenter.ActiveRow);
            Assert.AreEqual(0, _progressView.Completed);
            Assert.AreEqual(SubstationProcedureFactory.StepCount, _progressView.Total);
        }

        [Test]
        public void Constructor_RejectsMismatchedLengths()
        {
            Assert.Throws<ArgumentException>(() =>
                new ChecklistPresenterCore(new[] { 1, 2 }, new IChecklistItemView[1], null));
        }

        [Test]
        public void StartProcedure_MarksFirstRowActiveAndRestPending()
        {
            _director.StartProcedure();

            Assert.AreEqual(ProcedureStepState.Active, _views[0].State);
            for (int i = 1; i < _views.Length; i++)
                Assert.AreEqual(ProcedureStepState.Pending, _views[i].State);

            Assert.AreEqual(0, _presenter.ActiveRow);
            Assert.AreEqual(0, _progressView.ActiveRow);
            Assert.AreEqual(0, _progressView.Completed);
            Assert.AreEqual(SubstationProcedureFactory.StepCount, _progressView.Total);
        }

        [Test]
        public void MatchingSignal_CompletesCurrentRowAndActivatesNext()
        {
            _director.StartProcedure();

            _input.Raise(InteractionSignalIds.BajarSwitchPrincipal);

            Assert.AreEqual(ProcedureStepState.Completed, _views[0].State);
            Assert.AreEqual(ProcedureStepState.Active, _views[1].State);
            Assert.AreEqual(ProcedureStepState.Pending, _views[2].State);
            Assert.AreEqual(1, _presenter.ActiveRow);
            Assert.AreEqual(1, _progressView.ActiveRow);
            Assert.AreEqual(1, _progressView.Completed);
        }

        [Test]
        public void OutOfOrderSignal_DoesNotTouchViews()
        {
            _director.StartProcedure();
            int[] callsBefore = SnapshotCalls();
            int progressCallsBefore = _progressView.ProgressCalls;

            _input.Raise(InteractionSignalIds.ColocarCandado);

            CollectionAssert.AreEqual(callsBefore, SnapshotCalls());
            Assert.AreEqual(progressCallsBefore, _progressView.ProgressCalls);
            Assert.AreEqual(ProcedureStepState.Active, _views[0].State);
        }

        [Test]
        public void EachTransition_WritesOnlyTheRowsThatChanged()
        {
            _director.StartProcedure();
            int[] before = SnapshotCalls();

            _input.Raise(InteractionSignalIds.BajarSwitchPrincipal);

            int[] after = SnapshotCalls();
            for (int i = 0; i < after.Length; i++)
            {
                int expectedDelta = i <= 1 ? 1 : 0;
                Assert.AreEqual(expectedDelta, after[i] - before[i], "Fila " + i);
            }
        }

        [Test]
        public void FullProcedure_MarksAllRowsCompletedAndFlagsCompletion()
        {
            _director.StartProcedure();
            RunFullProcedure();

            for (int i = 0; i < _views.Length; i++)
                Assert.AreEqual(ProcedureStepState.Completed, _views[i].State);

            Assert.IsTrue(_presenter.IsProcedureComplete);
            Assert.IsTrue(_progressView.Complete);
            Assert.AreEqual(ChecklistPresenterCore.NoRow, _progressView.ActiveRow);
            Assert.AreEqual(SubstationProcedureFactory.StepCount, _progressView.Completed);
        }

        [Test]
        public void RestartProcedure_ResetsRowsAndCompletionFlag()
        {
            _director.StartProcedure();
            RunFullProcedure();

            _director.StartProcedure();

            Assert.IsFalse(_presenter.IsProcedureComplete);
            Assert.IsFalse(_progressView.Complete);
            Assert.AreEqual(ProcedureStepState.Active, _views[0].State);
            for (int i = 1; i < _views.Length; i++)
                Assert.AreEqual(ProcedureStepState.Pending, _views[i].State);
            Assert.AreEqual(0, _progressView.Completed);
        }

        [Test]
        public void PartialRowBinding_IgnoresUnboundSteps()
        {
            var colocarCandado = new FakeItemView();
            var partial = new ChecklistPresenterCore(
                new[] { ProcedureStepIds.ColocarCandado },
                new IChecklistItemView[] { colocarCandado },
                null);
            partial.Bind(_progress);

            _director.StartProcedure();
            Assert.AreEqual(ChecklistPresenterCore.NoRow, partial.ActiveRow);

            _input.Raise(InteractionSignalIds.BajarSwitchPrincipal);
            _input.Raise(InteractionSignalIds.BajarProtectores);
            _input.Raise(InteractionSignalIds.ApagarSubestacion_BotonRojo);
            _input.Raise(InteractionSignalIds.SubirProtectores);

            Assert.AreEqual(ProcedureStepState.Active, colocarCandado.State);
            Assert.AreEqual(0, partial.ActiveRow);

            _input.Raise(InteractionSignalIds.ColocarCandado);

            Assert.AreEqual(ProcedureStepState.Completed, colocarCandado.State);
            Assert.AreEqual(ChecklistPresenterCore.NoRow, partial.ActiveRow);

            partial.Unbind();
        }

        [Test]
        public void UnboundPresenter_StopsReceivingUpdates()
        {
            _presenter.Unbind();

            _director.StartProcedure();
            _input.Raise(InteractionSignalIds.BajarSwitchPrincipal);

            Assert.IsFalse(_presenter.IsBound);
            Assert.AreEqual(0, _progress.ListenerCount);
            for (int i = 0; i < _views.Length; i++)
                Assert.AreEqual(ProcedureStepState.Pending, _views[i].State);
        }

        [Test]
        public void FullProcedureCycle_DoesNotAllocateAfterWarmup()
        {
            _director.StartProcedure();
            RunFullProcedure();

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int cycle = 0; cycle < 8; cycle++)
            {
                _director.StartProcedure();
                RunFullProcedure();
            }
            long after = GC.GetAllocatedBytesForCurrentThread();

            Assert.AreEqual(before, after, "El flujo director -> canal -> presenter no debe asignar en heap.");
        }

        [Test]
        public void Style_ResolvesLegacyChecklistPaletteByState()
        {
            var style = new ChecklistStyle();

            ChecklistVisual pending = style.Resolve(ProcedureStepState.Pending);
            ChecklistVisual active = style.Resolve(ProcedureStepState.Active);
            ChecklistVisual completed = style.Resolve(ProcedureStepState.Completed);

            Assert.AreEqual(Color.gray, pending.Color);
            Assert.AreEqual(FontStyles.Normal, pending.FontStyle);
            Assert.IsFalse(pending.IconVisible);

            Assert.AreEqual(Color.yellow, active.Color);
            Assert.AreEqual(FontStyles.Bold, active.FontStyle);
            Assert.IsTrue(active.IconVisible);

            Assert.AreEqual(Color.green, completed.Color);
            Assert.AreEqual(FontStyles.Strikethrough, completed.FontStyle);
            Assert.IsTrue(completed.IconVisible);
        }

        [Test]
        public void TmpItemView_AppliesStyleToLabelAndIcon()
        {
            var go = new GameObject("ChecklistRowTest", typeof(RectTransform));
            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(go.transform, false);

            try
            {
                var label = go.AddComponent<TextMeshProUGUI>();
                var icon = iconGo.AddComponent<Image>();
                var view = go.AddComponent<TmpChecklistItemView>();
                var style = new ChecklistStyle();
                view.Configure(label, icon, style);

                view.SetState(ProcedureStepState.Pending);
                Assert.AreEqual(style.pendingColor, label.color);
                Assert.AreEqual(FontStyles.Normal, label.fontStyle);
                Assert.IsFalse(icon.enabled);

                view.SetState(ProcedureStepState.Active);
                Assert.AreEqual(style.activeColor, label.color);
                Assert.AreEqual(FontStyles.Bold, label.fontStyle);
                Assert.IsTrue(icon.enabled);
                Assert.AreEqual(style.activeColor, icon.color);

                view.SetState(ProcedureStepState.Completed);
                Assert.AreEqual(style.completedColor, label.color);
                Assert.AreEqual(FontStyles.Strikethrough, label.fontStyle);
                Assert.IsTrue(icon.enabled);
                Assert.AreEqual(ProcedureStepState.Completed, view.State);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private void RunFullProcedure()
        {
            int[] ids = SubstationProcedureFactory.OrderedSignalIds;
            for (int i = 0; i < ids.Length; i++)
                _input.Raise(ids[i]);
        }

        private int[] SnapshotCalls()
        {
            var calls = new int[_views.Length];
            for (int i = 0; i < _views.Length; i++)
                calls[i] = _views[i].SetStateCalls;
            return calls;
        }
    }
}
