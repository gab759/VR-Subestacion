using UnityEngine;

namespace VRSubestacion.Simulation.Integration
{
    /// <summary>
    /// Agrupa miembros (palancas, protectores, barras) y emite una señal cuando todos quedan ON
    /// o todos vuelven a OFF. Los miembros se registran solos al habilitarse.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ToggleSignalGate : MonoBehaviour
    {
        [SerializeField, Min(1)] private int expectedMembers = 1;
        [SerializeField] private SignalOutput onAllOn = new SignalOutput();
        [SerializeField] private SignalOutput onAllOff = new SignalOutput();

        private ToggleGroupLatch _latch;

        public int RegisteredMembers => _latch != null ? _latch.RegisteredCount : 0;
        public int OnMembers => _latch != null ? _latch.OnCount : 0;

        public int RegisterMember()
        {
            EnsureLatch();
            int index = _latch.Register();

            if (index < 0)
                Debug.LogWarning($"[ToggleSignalGate] {name}: se registraron más miembros que expectedMembers ({expectedMembers}).", this);

            return index;
        }

        public void SetMemberState(int index, bool on)
        {
            EnsureLatch();

            switch (_latch.Set(index, on))
            {
                case ToggleGroupLatch.Transition.AllOn:
                    onAllOn.Emit();
                    break;
                case ToggleGroupLatch.Transition.AllOff:
                    onAllOff.Emit();
                    break;
            }
        }

        private void EnsureLatch()
        {
            if (_latch == null)
                _latch = new ToggleGroupLatch(expectedMembers);
        }
    }
}
