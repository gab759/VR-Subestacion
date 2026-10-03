namespace VRSubestacion.Simulation
{
    /// <summary>
    /// Alias serializable de <see cref="InteractionSignalIds"/> para configurar señales desde el Inspector.
    /// </summary>
    public enum ProcedureSignal
    {
        None = InteractionSignalIds.None,
        BajarSwitchPrincipal = InteractionSignalIds.BajarSwitchPrincipal,
        BajarProtectores = InteractionSignalIds.BajarProtectores,
        ApagarSubestacion_BotonRojo = InteractionSignalIds.ApagarSubestacion_BotonRojo,
        SubirProtectores = InteractionSignalIds.SubirProtectores,
        ColocarCandado = InteractionSignalIds.ColocarCandado,
        VerificarBarrasPuerta1_Abajo = InteractionSignalIds.VerificarBarrasPuerta1_Abajo,
        VerificarBarrasPuerta1_Arriba = InteractionSignalIds.VerificarBarrasPuerta1_Arriba,
        VerificarBarrasPuerta2 = InteractionSignalIds.VerificarBarrasPuerta2,
        VerificarChocolatito = InteractionSignalIds.VerificarChocolatito,
        BajarPalancasPuerta1 = InteractionSignalIds.BajarPalancasPuerta1,
        ColocarPulpo_Tierra = InteractionSignalIds.ColocarPulpo_Tierra,
        ColocarPulpo_Barras = InteractionSignalIds.ColocarPulpo_Barras,
        SenalizarZona = InteractionSignalIds.SenalizarZona,
        RetirarPulpo = InteractionSignalIds.RetirarPulpo,
        SubirPalancasPuerta1 = InteractionSignalIds.SubirPalancasPuerta1,
        SubirSwitchPrincipal = InteractionSignalIds.SubirSwitchPrincipal,
        QuitarCandado = InteractionSignalIds.QuitarCandado,
        EncenderSubestacion_BotonRojo = InteractionSignalIds.EncenderSubestacion_BotonRojo
    }
}
