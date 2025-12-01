using UnityEngine;

public class SubstationProcedureManager : MonoBehaviour
{
    [Header("Checklist")]
    [SerializeField] private CheckList checklist;

    [Header("Desconexión inicial")]
    [SerializeField] private string step_BajarSwitchPrincipal = "BajarSwitchPrincipal";
    [SerializeField] private string step_BajarProtectores = "BajarProtectores";
    [SerializeField] private string step_ApagarSubestacion_BotonRojo = "ApagarSubestacion_BotonRojo";
    [SerializeField] private string step_SubirProtectores = "SubirProtectores";
    [SerializeField] private string step_ColocarCandado = "ColocarCandado";

    [Header("Verificación de ausencia de tensión")]
    [SerializeField] private string step_VerificarPuerta1Abajo = "VerificarBarrasPuerta1_Abajo";
    [SerializeField] private string step_VerificarPuerta1Arriba = "VerificarBarrasPuerta1_Arriba";
    [SerializeField] private string step_VerificarPuerta2 = "VerificarBarrasPuerta2";
    [SerializeField] private string step_VerificarChocolatito = "VerificarChocolatito";

    [Header("Palancas / Pulpo / Señalización")]
    [SerializeField] private string step_BajarPalancasPuerta1 = "BajarPalancasPuerta1";
    [SerializeField] private string step_ColocarPulpo_Tierra = "ColocarPulpo_Tierra";
    [SerializeField] private string step_ColocarPulpo_Barras = "ColocarPulpo_Barras";
    [SerializeField] private string step_SenalizarZona = "SenalizarZona";
    [SerializeField] private string step_RetirarPulpo = "RetirarPulpo";
    [SerializeField] private string step_SubirPalancasPuerta1 = "SubirPalancasPuerta1";

    [Header("Re-energización")]
    [SerializeField] private string step_SubirSwitchPrincipal = "SubirSwitchPrincipal";
    [SerializeField] private string step_QuitarCandado = "QuitarCandado";
    [SerializeField] private string step_EncenderSubestacion_BotonRojo = "EncenderSubestacion_BotonRojo";

    [Header("Comportamiento")]
    [SerializeField] private bool penalizeOutOfOrder = true;

    // --- núcleo de evaluación ---

    public void EvaluateStep(string expectedStepName)
    {
        if (checklist == null || string.IsNullOrEmpty(expectedStepName))
            return;

        string current = checklist.GetCurrentItemName();

        if (current == expectedStepName)
        {
            checklist.CompleteItem(expectedStepName);

            if (GameScoreManager.Instance != null)
                GameScoreManager.Instance.RegisterCorrect(ScoreCategory.Mantenimiento);

            Debug.Log($"[Substation] Paso correcto: {expectedStepName}");
        }
        else
        {
            if (penalizeOutOfOrder && GameScoreManager.Instance != null)
                GameScoreManager.Instance.RegisterMistake(ScoreCategory.Mantenimiento);

            Debug.LogWarning($"[Substation] Paso fuera de orden. Actual: {current}, acción: {expectedStepName}");
        }
    }

    // --- funciones para enganchar desde otros scripts / eventos ---

    // Desconexión inicial
    public void OnSwitchPrincipalBajado() =>
        EvaluateStep(step_BajarSwitchPrincipal);

    public void OnProtectoresBajados() =>
        EvaluateStep(step_BajarProtectores);

    public void OnBotonRojo_Apagar() =>
        EvaluateStep(step_ApagarSubestacion_BotonRojo);

    public void OnProtectoresSubidos() =>
        EvaluateStep(step_SubirProtectores);

    public void OnCandadoColocado() =>
        EvaluateStep(step_ColocarCandado);

    // Verificación tensión
    public void OnDetector_Puerta1AbajoCompletado() =>
        EvaluateStep(step_VerificarPuerta1Abajo);

    public void OnDetector_Puerta1ArribaCompletado() =>
        EvaluateStep(step_VerificarPuerta1Arriba);

    public void OnDetector_Puerta2Completado() =>
        EvaluateStep(step_VerificarPuerta2);

    public void OnDetector_ChocolatitoCompletado() =>
        EvaluateStep(step_VerificarChocolatito);

    // Palancas / pulpo / señalización
    public void OnPalancasPuerta1_Bajadas() =>
        EvaluateStep(step_BajarPalancasPuerta1);

    public void OnPulpo_ConectadoTierra() =>
        EvaluateStep(step_ColocarPulpo_Tierra);

    public void OnPulpo_ConectadoBarras() =>
        EvaluateStep(step_ColocarPulpo_Barras);

    public void OnZona_Senalizada() =>
        EvaluateStep(step_SenalizarZona);

    public void OnPulpo_Retirado() =>
        EvaluateStep(step_RetirarPulpo);

    public void OnPalancasPuerta1_Subidas() =>
        EvaluateStep(step_SubirPalancasPuerta1);

    // Re-energización
    public void OnSwitchPrincipalSubido() =>
        EvaluateStep(step_SubirSwitchPrincipal);

    public void OnCandado_Quitado() =>
        EvaluateStep(step_QuitarCandado);

    public void OnBotonRojo_Encender() =>
        EvaluateStep(step_EncenderSubestacion_BotonRojo);
}
