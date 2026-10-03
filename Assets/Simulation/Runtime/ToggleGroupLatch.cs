namespace VRSubestacion.Simulation
{
    /// <summary>
    /// Agrega N miembros binarios (palancas, protectores, barras verificadas) y reporta
    /// cuándo todos quedan activos, y cuándo todos vuelven a inactivo después de haberlo estado.
    /// </summary>
    public sealed class ToggleGroupLatch
    {
        public enum Transition
        {
            None,
            AllOn,
            AllOff
        }

        private readonly bool[] _states;
        private int _registered;
        private int _onCount;
        private bool _armedForAllOff;

        public ToggleGroupLatch(int capacity)
        {
            _states = new bool[capacity < 1 ? 1 : capacity];
        }

        public int Capacity => _states.Length;
        public int RegisteredCount => _registered;
        public int OnCount => _onCount;

        public int Register()
        {
            if (_registered >= _states.Length)
                return -1;

            return _registered++;
        }

        /// <summary>
        /// Reportar <c>on = true</c> con el grupo ya completo vuelve a devolver
        /// <see cref="Transition.AllOn"/>, para poder reintentar un paso que se hizo fuera de orden.
        /// </summary>
        public Transition Set(int index, bool on)
        {
            if (index < 0 || index >= _registered)
                return Transition.None;

            bool changed = _states[index] != on;
            if (changed)
            {
                _states[index] = on;
                _onCount += on ? 1 : -1;
            }

            if (on && _onCount == _states.Length)
            {
                _armedForAllOff = true;
                return Transition.AllOn;
            }

            if (!on && changed && _onCount == 0 && _armedForAllOff)
            {
                _armedForAllOff = false;
                return Transition.AllOff;
            }

            return Transition.None;
        }
    }
}
