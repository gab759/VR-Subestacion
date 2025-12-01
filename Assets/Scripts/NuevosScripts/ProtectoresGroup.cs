using UnityEngine;

public class ProtectoresGroup : MonoBehaviour
{
    [SerializeField] private SlidingDoor[] protectores;
    [SerializeField] private SubstationProcedureManager procedureManager;

    [Header("Nombres de pasos en el CheckList")]
    [SerializeField] private string stepAllDown = "BajarProtectores";
    [SerializeField] private string stepAllUp = "SubirProtectores";

    [Header("Candado")]
    [Tooltip("GameObject que tiene el PadlockReceiver (collider entre protectores). Se activará al completar SubirProtectores.")]
    [SerializeField] private GameObject padlockReceiverObject;

    private bool bajarStepDone = false;
    private bool subirStepDone = false;

    private void Start()
    {
        // Asegurarnos de que el receptor del candado está desactivado al inicio
        if (padlockReceiverObject != null)
            padlockReceiverObject.SetActive(false);
    }

    // Llamar este método desde onOpen/onClose de cada protector (SlidingDoor)
    public void OnProtectorToggled()
    {
        if (protectores == null || protectores.Length == 0 || procedureManager == null)
            return;

        bool allDown = true;
        bool allUp = true;

        foreach (var p in protectores)
        {
            if (p == null) continue;

            // Convención: IsOpen == protector BAJADO
            if (!p.IsOpen)
                allDown = false;
            if (p.IsOpen)
                allUp = false;
        }

        if (allDown && !bajarStepDone)
        {
            bajarStepDone = true;
            procedureManager.EvaluateStep(stepAllDown);
            Debug.Log("[ProtectoresGroup] Todos los protectores BAJADOS.");
        }

        if (allUp && bajarStepDone && !subirStepDone)
        {
            subirStepDone = true;
            procedureManager.EvaluateStep(stepAllUp);
            Debug.Log("[ProtectoresGroup] Todos los protectores SUBIDOS.");

            // 👉 Aquí activamos el GameObject del candado
            if (padlockReceiverObject != null)
            {
                padlockReceiverObject.SetActive(true);
                Debug.Log("[ProtectoresGroup] PadlockReceiver activado.");
            }
        }
    }
}
