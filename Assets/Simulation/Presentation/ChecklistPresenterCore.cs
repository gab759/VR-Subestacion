using System;
using VRSubestacion.Simulation;

namespace VRSubestacion.Presentation
{
    /// <summary>
    /// Lógica de presentación sin MonoBehaviour: traduce eventos de <see cref="ProcedureProgressChannel"/>
    /// a estados por fila. Solo conoce StepIds; no referencia al director ni a la interacción.
    /// Solo escribe en las vistas cuando el estado cambia.
    /// </summary>
    public sealed class ChecklistPresenterCore : IProcedureProgressListener
    {
        public const int NoRow = -1;

        private readonly int[] _stepIds;
        private readonly IChecklistItemView[] _views;
        private readonly ProcedureStepState[] _states;
        private readonly IChecklistProgressView _progressView;
        private readonly int _rowCount;

        private ProcedureProgressChannel _channel;
        private int _activeRow;
        private int _completedCount;
        private int _stepCount;
        private bool _isComplete;

        public ChecklistPresenterCore(int[] stepIds, IChecklistItemView[] views, IChecklistProgressView progressView)
        {
            if (stepIds == null)
                throw new ArgumentNullException(nameof(stepIds));
            if (views == null)
                throw new ArgumentNullException(nameof(views));
            if (stepIds.Length != views.Length)
                throw new ArgumentException("stepIds y views deben tener la misma longitud.", nameof(views));

            _rowCount = stepIds.Length;
            _stepIds = new int[_rowCount];
            _views = new IChecklistItemView[_rowCount];
            _states = new ProcedureStepState[_rowCount];
            Array.Copy(stepIds, _stepIds, _rowCount);
            Array.Copy(views, _views, _rowCount);
            _progressView = progressView;
            _stepCount = _rowCount;

            ResetView();
        }

        public int RowCount => _rowCount;
        public int ActiveRow => _activeRow;
        public int CompletedCount => _completedCount;
        public int StepCount => _stepCount;
        public bool IsProcedureComplete => _isComplete;
        public bool IsBound => _channel != null;

        public ProcedureStepState GetState(int row)
        {
            return _states[row];
        }

        public int FindRow(int stepId)
        {
            if (stepId == ProcedureStepIds.None)
                return NoRow;

            for (int i = 0; i < _rowCount; i++)
            {
                if (_stepIds[i] == stepId)
                    return i;
            }

            return NoRow;
        }

        public void Bind(ProcedureProgressChannel channel)
        {
            if (ReferenceEquals(_channel, channel))
                return;

            if (_channel != null)
                _channel.Unsubscribe(this);

            _channel = channel;

            if (_channel != null)
                _channel.Subscribe(this);
        }

        public void Unbind()
        {
            Bind(null);
        }

        public void ResetView()
        {
            for (int i = 0; i < _rowCount; i++)
            {
                _states[i] = ProcedureStepState.Pending;
                if (_views[i] != null)
                    _views[i].SetState(ProcedureStepState.Pending);
            }

            _activeRow = NoRow;
            _completedCount = 0;
            _isComplete = false;

            if (_progressView != null)
            {
                _progressView.SetProcedureComplete(false);
                _progressView.SetActiveRow(NoRow);
                _progressView.SetProgress(0, _stepCount);
            }
        }

        void IProcedureProgressListener.OnProgress(in ProcedureProgress progress)
        {
            switch (progress.Kind)
            {
                case ProcedureProgressKind.ProcedureStarted:
                    _stepCount = progress.StepCount;
                    ResetView();
                    break;

                case ProcedureProgressKind.StepStateChanged:
                    ApplyStepChanged(in progress);
                    break;

                case ProcedureProgressKind.ProcedureCompleted:
                    ApplyCompleted(in progress);
                    break;
            }
        }

        private void ApplyStepChanged(in ProcedureProgress progress)
        {
            int row = FindRow(progress.StepId);

            if (row != NoRow && _states[row] != progress.State)
            {
                _states[row] = progress.State;
                if (_views[row] != null)
                    _views[row].SetState(progress.State);
            }

            if (progress.State == ProcedureStepState.Active)
                SetActiveRow(row);
            else if (row != NoRow && row == _activeRow)
                SetActiveRow(NoRow);

            SetProgress(progress.CompletedCount, progress.StepCount);
        }

        private void ApplyCompleted(in ProcedureProgress progress)
        {
            SetActiveRow(NoRow);
            SetProgress(progress.CompletedCount, progress.StepCount);

            if (_isComplete)
                return;

            _isComplete = true;
            if (_progressView != null)
                _progressView.SetProcedureComplete(true);
        }

        private void SetActiveRow(int row)
        {
            if (_activeRow == row)
                return;

            _activeRow = row;
            if (_progressView != null)
                _progressView.SetActiveRow(row);
        }

        private void SetProgress(int completedCount, int stepCount)
        {
            if (_completedCount == completedCount && _stepCount == stepCount)
                return;

            _completedCount = completedCount;
            _stepCount = stepCount;
            if (_progressView != null)
                _progressView.SetProgress(completedCount, stepCount);
        }
    }
}
