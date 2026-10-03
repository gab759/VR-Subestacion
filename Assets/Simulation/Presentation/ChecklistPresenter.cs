using System;
using TMPro;
using UnityEngine;
using VRSubestacion.Simulation;

namespace VRSubestacion.Presentation
{
    /// <summary>
    /// Presenter en modo sombra: componente aditivo que convive con <c>CheckList</c> sin tocar
    /// su jerarquía. Se actualiza solo por eventos del canal; no tiene Update.
    /// Todos los strings se construyen en Awake o al cambiar el total de pasos.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("VR Subestacion/Checklist Presenter")]
    public sealed class ChecklistPresenter : MonoBehaviour, IChecklistProgressView
    {
        [Serializable]
        public struct Row
        {
            public ProcedureStep step;
            public TmpChecklistItemView view;

            [Tooltip("Texto del paso activo. Si está vacío se usa el DisplayName de la vista o el nombre del paso.")]
            public string title;
        }

        [SerializeField] private ProcedureProgressChannel progressChannel;
        [SerializeField] private Row[] rows = Array.Empty<Row>();

        [Header("Etiquetas opcionales")]
        [SerializeField] private TMP_Text progressLabel;
        [SerializeField] private string progressFormat = "{0}/{1}";
        [SerializeField] private TMP_Text activeStepLabel;
        [SerializeField] private string noActiveStepText = "";
        [SerializeField] private string completedText = "Procedimiento completado";

        private ChecklistPresenterCore _core;
        private string[] _rowTitles;
        private string[] _progressTexts;
        private int _cachedTotal = -1;
        private bool _complete;

        public ChecklistPresenterCore Core => _core;

        private void Awake()
        {
            EnsureCore();
        }

        private void OnEnable()
        {
            EnsureCore();
            _core.Bind(progressChannel);
        }

        private void OnDisable()
        {
            if (_core != null)
                _core.Unbind();
        }

        private void EnsureCore()
        {
            if (_core != null)
                return;

            int count = rows != null ? rows.Length : 0;
            var stepIds = new int[count];
            var views = new IChecklistItemView[count];
            _rowTitles = new string[count];

            for (int i = 0; i < count; i++)
            {
                stepIds[i] = (int)rows[i].step;
                views[i] = rows[i].view;
                _rowTitles[i] = ResolveTitle(in rows[i]);
            }

            _core = new ChecklistPresenterCore(stepIds, views, this);
        }

        private static string ResolveTitle(in Row row)
        {
            if (!string.IsNullOrEmpty(row.title))
                return row.title;
            if (row.view != null && !string.IsNullOrEmpty(row.view.DisplayName))
                return row.view.DisplayName;
            return row.step.ToString();
        }

        void IChecklistProgressView.SetProgress(int completedCount, int stepCount)
        {
            if (progressLabel == null)
                return;

            EnsureProgressTexts(stepCount);
            int index = completedCount < 0 ? 0 : (completedCount > stepCount ? stepCount : completedCount);
            progressLabel.text = _progressTexts[index];
        }

        void IChecklistProgressView.SetActiveRow(int row)
        {
            if (activeStepLabel == null)
                return;

            if (row >= 0 && row < _rowTitles.Length)
                activeStepLabel.text = _rowTitles[row];
            else
                activeStepLabel.text = _complete ? completedText : noActiveStepText;
        }

        void IChecklistProgressView.SetProcedureComplete(bool complete)
        {
            _complete = complete;

            if (activeStepLabel != null && _core != null && _core.ActiveRow == ChecklistPresenterCore.NoRow)
                activeStepLabel.text = complete ? completedText : noActiveStepText;
        }

        private void EnsureProgressTexts(int stepCount)
        {
            if (stepCount < 0)
                stepCount = 0;
            if (_cachedTotal == stepCount)
                return;

            _cachedTotal = stepCount;
            _progressTexts = new string[stepCount + 1];
            for (int i = 0; i <= stepCount; i++)
                _progressTexts[i] = string.Format(progressFormat, i, stepCount);
        }
    }
}
