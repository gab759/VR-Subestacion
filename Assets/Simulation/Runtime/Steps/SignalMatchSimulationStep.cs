namespace VRSubestacion.Simulation
{
    /// <summary>
    /// Paso que completa únicamente si el SignalId coincide. Sin asignaciones en Tick/TryAdvance.
    /// </summary>
    public abstract class SignalMatchSimulationStep : ISimulationStep
    {
        public abstract int StepId { get; }
        public abstract int ExpectedSignalId { get; }

        public virtual void OnEnter()
        {
        }

        public virtual void OnExit()
        {
        }

        public bool TryAdvance(in InteractionSignal signal)
        {
            return signal.SignalId == ExpectedSignalId;
        }

        public virtual void Tick(float deltaTime)
        {
        }
    }
}
