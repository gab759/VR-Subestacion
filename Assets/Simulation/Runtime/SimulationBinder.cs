using System;

namespace VRSubestacion.Simulation
{
    [Flags]
    public enum SimulationBindingIssues
    {
        None = 0,
        MissingInputChannel = 1 << 0,
        MissingProgressChannel = 1 << 1
    }

    public enum SimulationLifecycle : byte
    {
        Unbound = 0,
        Bound = 1,
        Running = 2
    }

    /// <summary>
    /// Ciclo de vida del director sin MonoBehaviour: Bind (canales) → StartProcedure → Tick → Unbind.
    /// Toda asignación ocurre en el constructor; Bind/Tick/Unbind no asignan en heap.
    /// </summary>
    public sealed class SimulationBinder
    {
        private readonly ProceduralDirector _director;
        private InteractionSignalChannel _input;
        private ProcedureProgressChannel _progress;
        private SimulationLifecycle _lifecycle;
        private SimulationBindingIssues _lastIssues;

        public SimulationBinder(ProceduralDirector director)
        {
            _director = director ?? throw new ArgumentNullException(nameof(director));
            _lifecycle = SimulationLifecycle.Unbound;
        }

        public static SimulationBinder CreateSubstation()
        {
            return new SimulationBinder(ProceduralDirector.CreateSubstationProcedure());
        }

        public ProceduralDirector Director => _director;
        public InteractionSignalChannel InputChannel => _input;
        public ProcedureProgressChannel ProgressChannel => _progress;
        public SimulationLifecycle Lifecycle => _lifecycle;
        public SimulationBindingIssues LastIssues => _lastIssues;
        public bool IsBound => _lifecycle != SimulationLifecycle.Unbound;

        public static SimulationBindingIssues Validate(InteractionSignalChannel input, ProcedureProgressChannel progress)
        {
            var issues = SimulationBindingIssues.None;
            if (input == null)
                issues |= SimulationBindingIssues.MissingInputChannel;
            if (progress == null)
                issues |= SimulationBindingIssues.MissingProgressChannel;
            return issues;
        }

        /// <summary>
        /// Sin canal de entrada no se enlaza. Sin canal de progreso se enlaza igual (la UI no se actualizará).
        /// </summary>
        public SimulationBindingIssues Bind(InteractionSignalChannel input, ProcedureProgressChannel progress)
        {
            _lastIssues = Validate(input, progress);

            if (input == null)
            {
                Unbind();
                return _lastIssues;
            }

            _input = input;
            _progress = progress;
            _director.Bind(input);
            _director.BindProgress(progress);
            _lifecycle = _director.IsStarted ? SimulationLifecycle.Running : SimulationLifecycle.Bound;

            return _lastIssues;
        }

        public bool StartProcedure()
        {
            if (!IsBound)
                return false;

            _director.StartProcedure();
            _lifecycle = SimulationLifecycle.Running;
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (_lifecycle == SimulationLifecycle.Running)
                _director.Tick(deltaTime);
        }

        public void Unbind()
        {
            _director.Unbind();
            _director.BindProgress(null);
            _input = null;
            _progress = null;
            _lifecycle = SimulationLifecycle.Unbound;
        }
    }
}
