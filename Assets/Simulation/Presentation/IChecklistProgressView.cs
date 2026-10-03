namespace VRSubestacion.Presentation
{
    public interface IChecklistProgressView
    {
        void SetProgress(int completedCount, int stepCount);

        /// <param name="row">Fila activa, o -1 si no hay paso en curso.</param>
        void SetActiveRow(int row);

        void SetProcedureComplete(bool complete);
    }
}
