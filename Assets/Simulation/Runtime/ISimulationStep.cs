namespace VRSubestacion.Simulation
{
    /// <summary>
    /// Contrato de un paso de procedimiento. <see cref="Tick"/> no debe asignar en heap.
    /// </summary>
    public interface ISimulationStep
    {
        int StepId { get; }
        int ExpectedSignalId { get; }

        void OnEnter();
        void OnExit();
        bool TryAdvance(in InteractionSignal signal);
        void Tick(float deltaTime);
    }
}
