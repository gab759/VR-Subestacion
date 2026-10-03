using System;
using UnityEngine;
using VRSubestacion.Presentation;

namespace VRSubestacion.Simulation.Integration
{
    /// <summary>
    /// Raíz de composición de la escena. Awake: crea el director (única asignación).
    /// OnEnable: inyecta el canal de progreso en los presenters y enlaza el director.
    /// Start: arranca el procedimiento, cuando todos los presenters ya están suscritos.
    /// OnDisable: desenlaza todo. Si este componente está deshabilitado, no toca nada de la escena.
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrder)]
    [DisallowMultipleComponent]
    [AddComponentMenu("VR Subestacion/Simulation Context")]
    public sealed class SimulationContext : MonoBehaviour
    {
        public const int ExecutionOrder = -500;

        [Header("Canales (assets del proyecto)")]
        [SerializeField] private InteractionSignalChannel inputChannel;
        [SerializeField] private ProcedureProgressChannel progressChannel;

        [Header("Presentación")]
        [SerializeField] private ChecklistPresenter[] presenters = Array.Empty<ChecklistPresenter>();

        [Header("Ciclo de vida")]
        [SerializeField] private bool startOnPlay = true;
        [SerializeField] private bool tickInUpdate = true;

        private SimulationBinder _binder;

        public InteractionSignalChannel InputChannel => inputChannel;
        public ProcedureProgressChannel ProgressChannel => progressChannel;
        public ChecklistPresenter[] Presenters => presenters;
        public SimulationBinder Binder => _binder;

        public void Configure(
            InteractionSignalChannel input,
            ProcedureProgressChannel progress,
            ChecklistPresenter[] targetPresenters)
        {
            inputChannel = input;
            progressChannel = progress;
            presenters = targetPresenters ?? Array.Empty<ChecklistPresenter>();

            if (_binder != null && isActiveAndEnabled)
                BindAll();
        }

        public void RestartProcedure()
        {
            if (_binder != null)
                _binder.StartProcedure();
        }

        private void Awake()
        {
            EnsureBinder();
        }

        private void OnEnable()
        {
            EnsureBinder();
            BindAll();
        }

        private void Start()
        {
            if (startOnPlay)
                _binder.StartProcedure();
        }

        private void Update()
        {
            if (tickInUpdate)
                _binder.Tick(Time.deltaTime);
        }

        private void OnDisable()
        {
            if (_binder != null)
                _binder.Unbind();
        }

        private void EnsureBinder()
        {
            if (_binder == null)
                _binder = SimulationBinder.CreateSubstation();
        }

        private void BindAll()
        {
            InjectPresenters(presenters, progressChannel);

            SimulationBindingIssues issues = _binder.Bind(inputChannel, progressChannel);
            if (issues != SimulationBindingIssues.None)
                Debug.LogError($"[SimulationContext] {name}: cableado incompleto ({issues}). Ejecuta 'VR Subestacion/Validation/Validate Simulation Wiring'.", this);
        }

        public static void InjectPresenters(ChecklistPresenter[] targets, ProcedureProgressChannel channel)
        {
            if (targets == null)
                return;

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null)
                    targets[i].SetChannel(channel);
            }
        }
    }
}
