using System;
using Oculus.Interaction;
using UnityEngine;
using VRSubestacion.Simulation.Integration;

namespace VRSubestacion.Adapters.MetaXR
{
    /// <summary>
    /// Barra a verificar con el detector de tensión: la punta debe permanecer en el trigger
    /// mientras el detector está agarrado con el Interaction SDK. Reporta a un ToggleSignalGate
    /// (todas las barras del grupo). Convive con VoltageDetectorTarget.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class ProbeDwellZoneAdapter : MonoBehaviour
    {
        [Tooltip("Collider de la punta del detector.")]
        [SerializeField] private Collider probeTip;

        [Tooltip("Opcional: interactable de agarre del detector. Si se asigna, solo cuenta mientras está agarrado.")]
        [SerializeField, Interface(typeof(IInteractableView)), Optional]
        private UnityEngine.Object probeGrabView;

        [SerializeField, Min(0f)] private float requiredDwellSeconds = 1f;

        [SerializeField] private ToggleSignalGate gate;
        [SerializeField] private SignalOutput onVerified = new SignalOutput();

        private IInteractableView _probeView;
        private Action<InteractableStateChangeArgs> _stateChangedHandler;
        private bool _probeHeld;
        private bool _probeInside;
        private bool _reportedThisContact;
        private float _dwell;
        private int _gateIndex = -1;

        public bool IsVerified { get; private set; }

        private void Awake()
        {
            _probeView = probeGrabView as IInteractableView;
            _stateChangedHandler = HandleProbeStateChanged;
        }

        private void OnEnable()
        {
            if (_probeView != null)
            {
                _probeView.WhenStateChanged += _stateChangedHandler;
                _probeHeld = _probeView.State == InteractableState.Select;
            }

            if (gate != null && _gateIndex < 0)
                _gateIndex = gate.RegisterMember();
        }

        private void OnDisable()
        {
            if (_probeView != null)
                _probeView.WhenStateChanged -= _stateChangedHandler;

            ResetContact();
        }

        private void Update()
        {
            if (!_probeInside || _reportedThisContact)
                return;

            if (_probeView != null && !_probeHeld)
            {
                _dwell = 0f;
                return;
            }

            _dwell += Time.deltaTime;
            if (_dwell < requiredDwellSeconds)
                return;

            _reportedThisContact = true;
            IsVerified = true;

            if (gate != null)
                gate.SetMemberState(_gateIndex, true);

            onVerified.Emit();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other != probeTip)
                return;

            _probeInside = true;
            _dwell = 0f;
            _reportedThisContact = false;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other == probeTip)
                ResetContact();
        }

        private void HandleProbeStateChanged(InteractableStateChangeArgs args)
        {
            _probeHeld = args.NewState == InteractableState.Select;
        }

        private void ResetContact()
        {
            _probeInside = false;
            _reportedThisContact = false;
            _dwell = 0f;
        }
    }
}
