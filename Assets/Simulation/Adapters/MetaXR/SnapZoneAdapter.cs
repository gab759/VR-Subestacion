using System;
using Oculus.Interaction;
using UnityEngine;
using VRSubestacion.Simulation.Integration;

namespace VRSubestacion.Adapters.MetaXR
{
    /// <summary>
    /// Zona de encaje (SnapInteractable): emite cuando un objeto queda encajado y cuando se retira.
    /// Sirve para candado, pulpo (tierra / barras) y señalización. Convive con PadlockReceiver.
    /// </summary>
    public sealed class SnapZoneAdapter : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IInteractableView))]
        private UnityEngine.Object snapZone;

        [Tooltip("Opcional: solo cuenta este SnapInteractor (p. ej. el del candado). Vacío = cualquiera.")]
        [SerializeField, Interface(typeof(IInteractorView)), Optional]
        private UnityEngine.Object acceptedInteractor;

        [SerializeField] private SignalOutput onSnapped = new SignalOutput();
        [SerializeField] private SignalOutput onUnsnapped = new SignalOutput();

        private IInteractableView _zone;
        private IInteractorView _accepted;
        private Action<IInteractorView> _addedHandler;
        private Action<IInteractorView> _removedHandler;
        private int _occupants;

        public bool IsOccupied => _occupants > 0;

        private void Awake()
        {
            _zone = snapZone as IInteractableView;
            _accepted = acceptedInteractor as IInteractorView;
            _addedHandler = HandleSelectingAdded;
            _removedHandler = HandleSelectingRemoved;
        }

        private void OnEnable()
        {
            _occupants = 0;
            if (_zone == null)
                return;

            _zone.WhenSelectingInteractorViewAdded += _addedHandler;
            _zone.WhenSelectingInteractorViewRemoved += _removedHandler;
        }

        private void OnDisable()
        {
            if (_zone != null)
            {
                _zone.WhenSelectingInteractorViewAdded -= _addedHandler;
                _zone.WhenSelectingInteractorViewRemoved -= _removedHandler;
            }

            _occupants = 0;
        }

        private bool Accepts(IInteractorView interactor)
        {
            return _accepted == null || ReferenceEquals(interactor, _accepted);
        }

        private void HandleSelectingAdded(IInteractorView interactor)
        {
            if (!Accepts(interactor))
                return;

            _occupants++;
            if (_occupants == 1)
                onSnapped.Emit();
        }

        private void HandleSelectingRemoved(IInteractorView interactor)
        {
            if (!Accepts(interactor) || _occupants == 0)
                return;

            _occupants--;
            if (_occupants == 0)
                onUnsnapped.Emit();
        }
    }
}
