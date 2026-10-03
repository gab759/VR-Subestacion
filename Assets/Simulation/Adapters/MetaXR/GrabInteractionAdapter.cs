using System;
using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;
using VRSubestacion.Simulation.Integration;

namespace VRSubestacion.Adapters.MetaXR
{
    /// <summary>
    /// Escucha uno o varios interactables de agarre (Grab, HandGrab, DistanceGrab...) y emite
    /// señales al tomar y soltar. Componente nuevo: convive con PadlockGrabber / DetectorGrabber.
    /// </summary>
    public sealed class GrabInteractionAdapter : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IInteractableView))]
        private List<UnityEngine.Object> interactableViews = new List<UnityEngine.Object>();

        [SerializeField] private SignalOutput onGrabbed = new SignalOutput();
        [SerializeField] private SignalOutput onReleased = new SignalOutput();

        private IInteractableView[] _views;
        private Action<InteractableStateChangeArgs> _stateChangedHandler;
        private int _selectingViews;

        public bool IsHeld => _selectingViews > 0;

        private void Awake()
        {
            _views = new IInteractableView[interactableViews.Count];
            for (int i = 0; i < interactableViews.Count; i++)
                _views[i] = interactableViews[i] as IInteractableView;

            _stateChangedHandler = HandleStateChanged;
        }

        private void OnEnable()
        {
            _selectingViews = 0;
            for (int i = 0; i < _views.Length; i++)
            {
                if (_views[i] == null)
                    continue;

                _views[i].WhenStateChanged += _stateChangedHandler;
                if (_views[i].State == InteractableState.Select)
                    _selectingViews++;
            }
        }

        private void OnDisable()
        {
            for (int i = 0; i < _views.Length; i++)
            {
                if (_views[i] != null)
                    _views[i].WhenStateChanged -= _stateChangedHandler;
            }

            _selectingViews = 0;
        }

        private void HandleStateChanged(InteractableStateChangeArgs args)
        {
            bool wasSelected = args.PreviousState == InteractableState.Select;
            bool isSelected = args.NewState == InteractableState.Select;

            if (!wasSelected && isSelected)
            {
                _selectingViews++;
                if (_selectingViews == 1)
                    onGrabbed.Emit();
            }
            else if (wasSelected && !isSelected && _selectingViews > 0)
            {
                _selectingViews--;
                if (_selectingViews == 0)
                    onReleased.Emit();
            }
        }
    }
}
