namespace VRSubestacion.Simulation
{
    /// <summary>
    /// Máquina de pasos desacoplada de UI y escena. Solo avanza al recibir
    /// <see cref="InteractionSignal"/> por canales ScriptableObject.
    /// </summary>
    public sealed class ProceduralDirector : IInteractionSignalListener
    {
        private readonly ISimulationStep[] _steps;
        private readonly int _stepCount;
        private InteractionSignalChannel _channel;
        private int _index;
        private bool _started;

        public ProceduralDirector(ISimulationStep[] steps)
        {
            _steps = steps;
            _stepCount = steps != null ? steps.Length : 0;
            _index = 0;
            _started = false;
        }

        public static ProceduralDirector CreateSubstationProcedure()
        {
            return new ProceduralDirector(SubstationProcedureFactory.CreateSteps());
        }

        public int StepCount => _stepCount;
        public int CurrentStepIndex => _index;
        public bool IsStarted => _started;
        public bool IsComplete => _started && _index >= _stepCount;

        public int CurrentStepId
        {
            get
            {
                if (!_started || IsComplete)
                    return ProcedureStepIds.None;
                return _steps[_index].StepId;
            }
        }

        public int CurrentExpectedSignalId
        {
            get
            {
                if (!_started || IsComplete)
                    return InteractionSignalIds.None;
                return _steps[_index].ExpectedSignalId;
            }
        }

        public void Bind(InteractionSignalChannel channel)
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

        public void StartProcedure()
        {
            _index = 0;
            _started = true;

            if (_stepCount > 0)
                _steps[0].OnEnter();
        }

        public void Tick(float deltaTime)
        {
            if (!_started || IsComplete)
                return;

            _steps[_index].Tick(deltaTime);
        }

        void IInteractionSignalListener.OnSignal(in InteractionSignal signal)
        {
            if (!_started || IsComplete)
                return;

            if (!_steps[_index].TryAdvance(in signal))
                return;

            _steps[_index].OnExit();
            _index++;

            if (!IsComplete)
                _steps[_index].OnEnter();
        }
    }
}
