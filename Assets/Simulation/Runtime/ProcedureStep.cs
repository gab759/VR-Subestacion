namespace VRSubestacion.Simulation
{
    /// <summary>
    /// Alias serializable de <see cref="ProcedureStepIds"/> para enlazar filas de UI desde el Inspector.
    /// </summary>
    public enum ProcedureStep
    {
        None = ProcedureStepIds.None,
        BajarSwitchPrincipal = ProcedureStepIds.BajarSwitchPrincipal,
        BajarProtectores = ProcedureStepIds.BajarProtectores,
        ApagarSubestacion_BotonRojo = ProcedureStepIds.ApagarSubestacion_BotonRojo,
        SubirProtectores = ProcedureStepIds.SubirProtectores,
        ColocarCandado = ProcedureStepIds.ColocarCandado,
        VerificarBarrasPuerta1_Abajo = ProcedureStepIds.VerificarBarrasPuerta1_Abajo,
        VerificarBarrasPuerta1_Arriba = ProcedureStepIds.VerificarBarrasPuerta1_Arriba,
        VerificarBarrasPuerta2 = ProcedureStepIds.VerificarBarrasPuerta2,
        VerificarChocolatito = ProcedureStepIds.VerificarChocolatito,
        BajarPalancasPuerta1 = ProcedureStepIds.BajarPalancasPuerta1,
        ColocarPulpo_Tierra = ProcedureStepIds.ColocarPulpo_Tierra,
        ColocarPulpo_Barras = ProcedureStepIds.ColocarPulpo_Barras,
        SenalizarZona = ProcedureStepIds.SenalizarZona,
        RetirarPulpo = ProcedureStepIds.RetirarPulpo,
        SubirPalancasPuerta1 = ProcedureStepIds.SubirPalancasPuerta1,
        SubirSwitchPrincipal = ProcedureStepIds.SubirSwitchPrincipal,
        QuitarCandado = ProcedureStepIds.QuitarCandado,
        EncenderSubestacion_BotonRojo = ProcedureStepIds.EncenderSubestacion_BotonRojo
    }
}
