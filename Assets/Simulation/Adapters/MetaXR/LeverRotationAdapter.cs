using System;
using Oculus.Interaction;
using UnityEngine;
using VRSubestacion.Simulation;
using VRSubestacion.Simulation.Integration;

namespace VRSubestacion.Adapters.MetaXR
{
    /// <summary>
    /// Palanca / protector / switch rotado por el Interaction SDK (p. ej. Grabbable + OneGrabRotateTransformer).
    /// Solo lee la rotación mientras está agarrado y emite al cruzar umbrales con histéresis.
    /// Componente nuevo: convive con SlidingDoor / LeverProcedureInteractable / ProcedureToggleInteractable.
    /// </summary>
    public sealed class LeverRotationAdapter : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IPointable))]
        private UnityEngine.Object grabPointable;

        [Tooltip("Transform que rota. Si está vacío se usa este mismo.")]
        [SerializeField] private Transform measuredTransform;

        [Tooltip("Eje local de giro. Invierte el signo para invertir el sentido de ON.")]
        [SerializeField] private Vector3 localAxis = Vector3.right;

        [SerializeField] private float onThresholdDegrees = 60f;
        [SerializeField] private float offThresholdDegrees = 20f;

        [SerializeField] private SignalOutput onSwitchedOn = new SignalOutput();
        [SerializeField] private SignalOutput onSwitchedOff = new SignalOutput();

        [Tooltip("Opcional: grupo que espera a que todas sus palancas cambien.")]
        [SerializeField] private ToggleSignalGate gate;

        private IPointable _pointable;
        private Action<PointerEvent> _pointerHandler;
        private readonly PointerSelectionTracker _selection = new PointerSelectionTracker();
        private Quaternion _restLocalRotation;
        private bool _isOn;
        private int _gateIndex = -1;

        public bool IsOn => _isOn;
        public float CurrentAngle => LeverAngleMath.SignedAngle(_restLocalRotation, measuredTransform.localRotation, localAxis);

        private void Awake()
        {
            if (measuredTransform == null)
                measuredTransform = transform;

            _restLocalRotation = measuredTransform.localRotation;
            _pointable = grabPointable as IPointable;
            _pointerHandler = HandlePointerEvent;
        }

        private void OnEnable()
        {
            if (_pointable != null)
                _pointable.WhenPointerEventRaised += _pointerHandler;

            if (gate != null && _gateIndex < 0)
                _gateIndex = gate.RegisterMember();
        }

        private void OnDisable()
        {
            if (_pointable != null)
                _pointable.WhenPointerEventRaised -= _pointerHandler;

            _selection.Clear();
        }

        private void OnValidate()
        {
            if (offThresholdDegrees > onThresholdDegrees)
                offThresholdDegrees = onThresholdDegrees;
        }

        private void HandlePointerEvent(PointerEvent evt)
        {
            switch (evt.Type)
            {
                case PointerEventType.Select:
                    _selection.Add(evt.Identifier);
                    break;
                case PointerEventType.Move:
                    if (_selection.IsActive)
                        Evaluate();
                    break;
                case PointerEventType.Unselect:
                case PointerEventType.Cancel:
                    if (_selection.Remove(evt.Identifier))
                        Evaluate();
                    break;
            }
        }

        private void Evaluate()
        {
            bool next = LeverAngleMath.EvaluateToggle(_isOn, CurrentAngle, onThresholdDegrees, offThresholdDegrees);
            if (next == _isOn)
                return;

            _isOn = next;

            if (_isOn)
                onSwitchedOn.Emit();
            else
                onSwitchedOff.Emit();

            if (gate != null)
                gate.SetMemberState(_gateIndex, _isOn);
        }
    }
}
