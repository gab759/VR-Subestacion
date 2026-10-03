namespace VRSubestacion.Simulation
{
    public sealed class BajarSwitchPrincipalStep : SignalMatchSimulationStep
    {
        public override int StepId => ProcedureStepIds.BajarSwitchPrincipal;
        public override int ExpectedSignalId => InteractionSignalIds.BajarSwitchPrincipal;
    }

    public sealed class BajarProtectoresStep : SignalMatchSimulationStep
    {
        public override int StepId => ProcedureStepIds.BajarProtectores;
        public override int ExpectedSignalId => InteractionSignalIds.BajarProtectores;
    }

    public sealed class ApagarSubestacionBotonRojoStep : SignalMatchSimulationStep
    {
        public override int StepId => ProcedureStepIds.ApagarSubestacion_BotonRojo;
        public override int ExpectedSignalId => InteractionSignalIds.ApagarSubestacion_BotonRojo;
    }

    public sealed class SubirProtectoresStep : SignalMatchSimulationStep
    {
        public override int StepId => ProcedureStepIds.SubirProtectores;
        public override int ExpectedSignalId => InteractionSignalIds.SubirProtectores;
    }

    public sealed class ColocarCandadoStep : SignalMatchSimulationStep
    {
        public override int StepId => ProcedureStepIds.ColocarCandado;
        public override int ExpectedSignalId => InteractionSignalIds.ColocarCandado;
    }

    public sealed class VerificarBarrasPuerta1AbajoStep : SignalMatchSimulationStep
    {
        public override int StepId => ProcedureStepIds.VerificarBarrasPuerta1_Abajo;
        public override int ExpectedSignalId => InteractionSignalIds.VerificarBarrasPuerta1_Abajo;
    }

    public sealed class VerificarBarrasPuerta1ArribaStep : SignalMatchSimulationStep
    {
        public override int StepId => ProcedureStepIds.VerificarBarrasPuerta1_Arriba;
        public override int ExpectedSignalId => InteractionSignalIds.VerificarBarrasPuerta1_Arriba;
    }

    public sealed class VerificarBarrasPuerta2Step : SignalMatchSimulationStep
    {
        public override int StepId => ProcedureStepIds.VerificarBarrasPuerta2;
        public override int ExpectedSignalId => InteractionSignalIds.VerificarBarrasPuerta2;
    }

    public sealed class VerificarChocolatitoStep : SignalMatchSimulationStep
    {
        public override int StepId => ProcedureStepIds.VerificarChocolatito;
        public override int ExpectedSignalId => InteractionSignalIds.VerificarChocolatito;
    }

    public sealed class BajarPalancasPuerta1Step : SignalMatchSimulationStep
    {
        public override int StepId => ProcedureStepIds.BajarPalancasPuerta1;
        public override int ExpectedSignalId => InteractionSignalIds.BajarPalancasPuerta1;
    }

    public sealed class ColocarPulpoTierraStep : SignalMatchSimulationStep
    {
        public override int StepId => ProcedureStepIds.ColocarPulpo_Tierra;
        public override int ExpectedSignalId => InteractionSignalIds.ColocarPulpo_Tierra;
    }

    public sealed class ColocarPulpoBarrasStep : SignalMatchSimulationStep
    {
        public override int StepId => ProcedureStepIds.ColocarPulpo_Barras;
        public override int ExpectedSignalId => InteractionSignalIds.ColocarPulpo_Barras;
    }

    public sealed class SenalizarZonaStep : SignalMatchSimulationStep
    {
        public override int StepId => ProcedureStepIds.SenalizarZona;
        public override int ExpectedSignalId => InteractionSignalIds.SenalizarZona;
    }

    public sealed class RetirarPulpoStep : SignalMatchSimulationStep
    {
        public override int StepId => ProcedureStepIds.RetirarPulpo;
        public override int ExpectedSignalId => InteractionSignalIds.RetirarPulpo;
    }

    public sealed class SubirPalancasPuerta1Step : SignalMatchSimulationStep
    {
        public override int StepId => ProcedureStepIds.SubirPalancasPuerta1;
        public override int ExpectedSignalId => InteractionSignalIds.SubirPalancasPuerta1;
    }

    public sealed class SubirSwitchPrincipalStep : SignalMatchSimulationStep
    {
        public override int StepId => ProcedureStepIds.SubirSwitchPrincipal;
        public override int ExpectedSignalId => InteractionSignalIds.SubirSwitchPrincipal;
    }

    public sealed class QuitarCandadoStep : SignalMatchSimulationStep
    {
        public override int StepId => ProcedureStepIds.QuitarCandado;
        public override int ExpectedSignalId => InteractionSignalIds.QuitarCandado;
    }

    public sealed class EncenderSubestacionBotonRojoStep : SignalMatchSimulationStep
    {
        public override int StepId => ProcedureStepIds.EncenderSubestacion_BotonRojo;
        public override int ExpectedSignalId => InteractionSignalIds.EncenderSubestacion_BotonRojo;
    }
}
