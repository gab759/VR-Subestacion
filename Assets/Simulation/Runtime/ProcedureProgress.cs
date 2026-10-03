namespace VRSubestacion.Simulation
{
    public enum ProcedureStepState : byte
    {
        Pending = 0,
        Active = 1,
        Completed = 2
    }

    public enum ProcedureProgressKind : byte
    {
        ProcedureStarted = 0,
        StepStateChanged = 1,
        ProcedureCompleted = 2
    }

    /// <summary>
    /// Evento de salida del director por valor. Sin heap: el canal lo pasa con <c>in</c>.
    /// </summary>
    public readonly struct ProcedureProgress
    {
        public readonly ProcedureProgressKind Kind;
        public readonly ProcedureStepState State;
        public readonly int StepId;
        public readonly int StepIndex;
        public readonly int CompletedCount;
        public readonly int StepCount;

        public ProcedureProgress(
            ProcedureProgressKind kind,
            int stepId,
            int stepIndex,
            ProcedureStepState state,
            int completedCount,
            int stepCount)
        {
            Kind = kind;
            StepId = stepId;
            StepIndex = stepIndex;
            State = state;
            CompletedCount = completedCount;
            StepCount = stepCount;
        }

        public static ProcedureProgress Started(int stepCount)
        {
            return new ProcedureProgress(
                ProcedureProgressKind.ProcedureStarted,
                ProcedureStepIds.None, -1, ProcedureStepState.Pending, 0, stepCount);
        }

        public static ProcedureProgress StepChanged(
            int stepId, int stepIndex, ProcedureStepState state, int completedCount, int stepCount)
        {
            return new ProcedureProgress(
                ProcedureProgressKind.StepStateChanged,
                stepId, stepIndex, state, completedCount, stepCount);
        }

        public static ProcedureProgress Completed(int stepCount)
        {
            return new ProcedureProgress(
                ProcedureProgressKind.ProcedureCompleted,
                ProcedureStepIds.None, -1, ProcedureStepState.Completed, stepCount, stepCount);
        }
    }
}
