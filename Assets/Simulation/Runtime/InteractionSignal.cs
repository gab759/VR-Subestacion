namespace VRSubestacion.Simulation
{
    /// <summary>
    /// Payload de interacción por valor. Sin heap: el bus y el director lo pasan con <c>in</c>.
    /// </summary>
    public readonly struct InteractionSignal
    {
        public readonly int SignalId;
        public readonly int Payload;

        public InteractionSignal(int signalId, int payload = 0)
        {
            SignalId = signalId;
            Payload = payload;
        }
    }
}
