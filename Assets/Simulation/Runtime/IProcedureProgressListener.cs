namespace VRSubestacion.Simulation
{
    public interface IProcedureProgressListener
    {
        void OnProgress(in ProcedureProgress progress);
    }
}
