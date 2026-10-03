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
        private ProcedureProgressChannel _progress;
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

        /// <summary>
        /// Canal de salida opcional: la UI observa el progreso sin referenciar al director.
        /// </summary>
        public void BindProgress(ProcedureProgressChannel progress)
        {
            _progress = progress;
        }

        public void StartProcedure()
        {
            using (new SimulationSampleScope(SimulationProfilerMarkers.Start))
            {
                _index = 0;
                _started = true;

                PublishProgress(ProcedureProgress.Started(_stepCount));

                if (_stepCount > 0)
                {
                    _steps[0].OnEnter();
                    PublishStep(0, ProcedureStepState.Active);
                }
                else
                {
                    PublishProgress(ProcedureProgress.Completed(_stepCount));
                }
            }
        }

        public void Tick(float deltaTime)
        {
            using (new SimulationSampleScope(SimulationProfilerMarkers.Tick))
            {
                if (!_started || IsComplete)
                    return;

                _steps[_index].Tick(deltaTime);
            }
        }

        void IInteractionSignalListener.OnSignal(in InteractionSignal signal)
        {
            using (new SimulationSampleScope(SimulationProfilerMarkers.Signal))
            {
                if (!_started || IsComplete)
                    return;

                if (!_steps[_index].TryAdvance(in signal))
                    return;

                _steps[_index].OnExit();
                int completedIndex = _index;
                _index++;

                PublishStep(completedIndex, ProcedureStepState.Completed);

                if (!IsComplete)
                {
                    _steps[_index].OnEnter();
                    PublishStep(_index, ProcedureStepState.Active);
                }
                else
                {
                    PublishProgress(ProcedureProgress.Completed(_stepCount));
                }
            }
        }

        private void PublishStep(int index, ProcedureStepState state)
        {
            int completed = state == ProcedureStepState.Completed ? index + 1 : index;
            PublishProgress(ProcedureProgress.StepChanged(
                _steps[index].StepId, index, state, completed, _stepCount));
        }

        private void PublishProgress(in ProcedureProgress progress)
        {
            if (_progress != null)
                _progress.Raise(in progress);
        }
    }
}
