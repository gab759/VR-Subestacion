using System;
using Oculus.Interaction;
using UnityEngine;
using VRSubestacion.Simulation.Integration;

namespace VRSubestacion.Adapters.MetaXR
{
    /// <summary>
    /// Botón físico: escucha un IPointable (PokeInteractable o RayInteractable) y emite al presionar.
    /// Componente nuevo: convive con PushButton.
    /// </summary>
    public sealed class PokeInteractionAdapter : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IPointable))]
        private UnityEngine.Object pointable;

        [SerializeField] private SignalOutput onPressed = new SignalOutput();
        [SerializeField] private SignalOutput onReleased = new SignalOutput();

        private IPointable _pointable;
        private Action<PointerEvent> _pointerHandler;
        private readonly PointerSelectionTracker _selection = new PointerSelectionTracker();

        public bool IsPressed => _selection.IsActive;

        private void Awake()
        {
            _pointable = pointable as IPointable;
            _pointerHandler = HandlePointerEvent;
        }

        private void OnEnable()
        {
            if (_pointable != null)
                _pointable.WhenPointerEventRaised += _pointerHandler;
        }

        private void OnDisable()
        {
            if (_pointable != null)
                _pointable.WhenPointerEventRaised -= _pointerHandler;

            _selection.Clear();
        }

        private void HandlePointerEvent(PointerEvent evt)
        {
            switch (evt.Type)
            {
                case PointerEventType.Select:
                    if (_selection.Add(evt.Identifier))
                        onPressed.Emit();
                    break;
                case PointerEventType.Unselect:
                case PointerEventType.Cancel:
                    if (_selection.Remove(evt.Identifier))
                        onReleased.Emit();
                    break;
            }
        }
    }
}
