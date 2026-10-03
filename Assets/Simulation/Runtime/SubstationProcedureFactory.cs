namespace VRSubestacion.Simulation
{
    /// <summary>
    /// Construye el arreglo de pasos una sola vez (construcción, no tick).
    /// </summary>
    public static class SubstationProcedureFactory
    {
        public const int StepCount = 18;

        public static readonly int[] OrderedSignalIds =
        {
            InteractionSignalIds.BajarSwitchPrincipal,
            InteractionSignalIds.BajarProtectores,
            InteractionSignalIds.ApagarSubestacion_BotonRojo,
            InteractionSignalIds.SubirProtectores,
            InteractionSignalIds.ColocarCandado,
            InteractionSignalIds.VerificarBarrasPuerta1_Abajo,
            InteractionSignalIds.VerificarBarrasPuerta1_Arriba,
            InteractionSignalIds.VerificarBarrasPuerta2,
            InteractionSignalIds.VerificarChocolatito,
            InteractionSignalIds.BajarPalancasPuerta1,
            InteractionSignalIds.ColocarPulpo_Tierra,
            InteractionSignalIds.ColocarPulpo_Barras,
            InteractionSignalIds.SenalizarZona,
            InteractionSignalIds.RetirarPulpo,
            InteractionSignalIds.SubirPalancasPuerta1,
            InteractionSignalIds.SubirSwitchPrincipal,
            InteractionSignalIds.QuitarCandado,
            InteractionSignalIds.EncenderSubestacion_BotonRojo
        };

        public static ISimulationStep[] CreateSteps()
        {
            return new ISimulationStep[]
            {
                new BajarSwitchPrincipalStep(),
                new BajarProtectoresStep(),
                new ApagarSubestacionBotonRojoStep(),
                new SubirProtectoresStep(),
                new ColocarCandadoStep(),
                new VerificarBarrasPuerta1AbajoStep(),
                new VerificarBarrasPuerta1ArribaStep(),
                new VerificarBarrasPuerta2Step(),
                new VerificarChocolatitoStep(),
                new BajarPalancasPuerta1Step(),
                new ColocarPulpoTierraStep(),
                new ColocarPulpoBarrasStep(),
                new SenalizarZonaStep(),
                new RetirarPulpoStep(),
                new SubirPalancasPuerta1Step(),
                new SubirSwitchPrincipalStep(),
                new QuitarCandadoStep(),
                new EncenderSubestacionBotonRojoStep()
            };
        }
    }
}
