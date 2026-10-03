namespace VRSubestacion.Simulation
{
    /// <summary>
    /// IDs estables de paso y de señal (1:1). Evitan strings en el tick y en las transiciones.
    /// El orden coincide con el checklist de GameSubestacion.
    /// </summary>
    public static class ProcedureStepIds
    {
        public const int None = 0;
        public const int BajarSwitchPrincipal = 1;
        public const int BajarProtectores = 2;
        public const int ApagarSubestacion_BotonRojo = 3;
        public const int SubirProtectores = 4;
        public const int ColocarCandado = 5;
        public const int VerificarBarrasPuerta1_Abajo = 6;
        public const int VerificarBarrasPuerta1_Arriba = 7;
        public const int VerificarBarrasPuerta2 = 8;
        public const int VerificarChocolatito = 9;
        public const int BajarPalancasPuerta1 = 10;
        public const int ColocarPulpo_Tierra = 11;
        public const int ColocarPulpo_Barras = 12;
        public const int SenalizarZona = 13;
        public const int RetirarPulpo = 14;
        public const int SubirPalancasPuerta1 = 15;
        public const int SubirSwitchPrincipal = 16;
        public const int QuitarCandado = 17;
        public const int EncenderSubestacion_BotonRojo = 18;
    }

    public static class InteractionSignalIds
    {
        public const int None = 0;
        public const int BajarSwitchPrincipal = ProcedureStepIds.BajarSwitchPrincipal;
        public const int BajarProtectores = ProcedureStepIds.BajarProtectores;
        public const int ApagarSubestacion_BotonRojo = ProcedureStepIds.ApagarSubestacion_BotonRojo;
        public const int SubirProtectores = ProcedureStepIds.SubirProtectores;
        public const int ColocarCandado = ProcedureStepIds.ColocarCandado;
        public const int VerificarBarrasPuerta1_Abajo = ProcedureStepIds.VerificarBarrasPuerta1_Abajo;
        public const int VerificarBarrasPuerta1_Arriba = ProcedureStepIds.VerificarBarrasPuerta1_Arriba;
        public const int VerificarBarrasPuerta2 = ProcedureStepIds.VerificarBarrasPuerta2;
        public const int VerificarChocolatito = ProcedureStepIds.VerificarChocolatito;
        public const int BajarPalancasPuerta1 = ProcedureStepIds.BajarPalancasPuerta1;
        public const int ColocarPulpo_Tierra = ProcedureStepIds.ColocarPulpo_Tierra;
        public const int ColocarPulpo_Barras = ProcedureStepIds.ColocarPulpo_Barras;
        public const int SenalizarZona = ProcedureStepIds.SenalizarZona;
        public const int RetirarPulpo = ProcedureStepIds.RetirarPulpo;
        public const int SubirPalancasPuerta1 = ProcedureStepIds.SubirPalancasPuerta1;
        public const int SubirSwitchPrincipal = ProcedureStepIds.SubirSwitchPrincipal;
        public const int QuitarCandado = ProcedureStepIds.QuitarCandado;
        public const int EncenderSubestacion_BotonRojo = ProcedureStepIds.EncenderSubestacion_BotonRojo;
    }
}
