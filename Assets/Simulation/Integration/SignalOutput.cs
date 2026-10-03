using System;
using UnityEngine;

namespace VRSubestacion.Simulation.Integration
{
    /// <summary>
    /// Destino serializable de un adaptador: canal + señales a emitir en orden.
    /// </summary>
    [Serializable]
    public sealed class SignalOutput
    {
        [SerializeField] private InteractionSignalChannel channel;

        [Tooltip("Se emiten en orden. No incluyas dos señales de pasos consecutivos: el director avanzaría ambos en un solo evento.")]
        [SerializeField] private ProcedureSignal[] signals = Array.Empty<ProcedureSignal>();

        [SerializeField] private int payload;

        public bool IsConfigured => channel != null && signals != null && signals.Length > 0;

        public void Emit()
        {
            if (channel == null || signals == null)
                return;

            for (int i = 0; i < signals.Length; i++)
            {
                if (signals[i] != ProcedureSignal.None)
                    channel.Raise((int)signals[i], payload);
            }
        }
    }
}
