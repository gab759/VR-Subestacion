using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRSubestacion.Simulation;

namespace VRSubestacion.Presentation
{
    /// <summary>
    /// Vista pasiva de una fila: solo aplica color, estilo de fuente e ícono cuando el presenter lo pide.
    /// </summary>
    [AddComponentMenu("VR Subestacion/Checklist Item View (TMP)")]
    public sealed class TmpChecklistItemView : MonoBehaviour, IChecklistItemView
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image icon;

        [Tooltip("Si está vacío no se toca el texto existente del label.")]
        [SerializeField] private string displayName;

        [SerializeField] private ChecklistStyle style = new ChecklistStyle();

        private bool _hasState;
        private ProcedureStepState _state;

        public ProcedureStepState State => _state;

        public string DisplayName => displayName;

        private void Awake()
        {
            if (label != null && !string.IsNullOrEmpty(displayName))
                label.text = displayName;
        }

        public void Configure(TMP_Text targetLabel, Image targetIcon, ChecklistStyle targetStyle)
        {
            label = targetLabel;
            icon = targetIcon;
            if (targetStyle != null)
                style = targetStyle;
            _hasState = false;
        }

        public void SetState(ProcedureStepState state)
        {
            if (_hasState && _state == state)
                return;

            _hasState = true;
            _state = state;

            ChecklistVisual visual = style.Resolve(state);

            if (label != null)
            {
                label.color = visual.Color;
                label.fontStyle = visual.FontStyle;
            }

            if (icon != null)
            {
                icon.enabled = visual.IconVisible;
                icon.color = visual.Color;
                if (visual.IconSprite != null)
                    icon.sprite = visual.IconSprite;
            }
        }
    }
}
