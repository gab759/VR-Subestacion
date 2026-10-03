namespace VRSubestacion.Simulation
{
    public interface IInteractionSignalListener
    {
        void OnSignal(in InteractionSignal signal);
    }
}
