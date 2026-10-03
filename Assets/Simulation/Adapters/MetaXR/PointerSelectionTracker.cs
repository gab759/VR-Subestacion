namespace VRSubestacion.Adapters.MetaXR
{
    /// <summary>
    /// Conjunto fijo de identificadores de interactor seleccionando. Sin heap tras construir.
    /// </summary>
    internal sealed class PointerSelectionTracker
    {
        private readonly int[] _ids;
        private int _count;

        public PointerSelectionTracker(int capacity = 4)
        {
            _ids = new int[capacity];
        }

        public bool IsActive => _count > 0;

        /// <returns>true si pasó de 0 a 1 selección.</returns>
        public bool Add(int identifier)
        {
            for (int i = 0; i < _count; i++)
            {
                if (_ids[i] == identifier)
                    return false;
            }

            if (_count == _ids.Length)
                return false;

            _ids[_count++] = identifier;
            return _count == 1;
        }

        /// <returns>true si pasó de 1 a 0 selecciones.</returns>
        public bool Remove(int identifier)
        {
            for (int i = 0; i < _count; i++)
            {
                if (_ids[i] != identifier)
                    continue;

                _ids[i] = _ids[--_count];
                return _count == 0;
            }

            return false;
        }

        public void Clear()
        {
            _count = 0;
        }
    }
}
