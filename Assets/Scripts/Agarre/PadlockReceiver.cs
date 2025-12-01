using UnityEngine;

public class PadlockReceiver : MonoBehaviour, IInteractable
{
    [Header("Punto donde colocar el candado")]
    [SerializeField] private Transform snapPoint;

    [Header("Candado a aceptar (opcional)")]
    [Tooltip("Si lo asignas, este será el candado que se colocará. Si lo dejas vacío, usará el que esté dentro del trigger.")]
    [SerializeField] private PadlockGrabber padlockRef;

    [Header("Checklist / Procedimiento")]
    [SerializeField] private SubstationProcedureManager procedureManager;
    [SerializeField] private CheckList checklist;
    [SerializeField] private string checklistStepName = "ColocarCandado";

    // Candado detectado dentro del trigger
    private PadlockGrabber padlockInTrigger;

    private void Awake()
    {
        if (snapPoint == null)
            snapPoint = transform;
    }

    // ---------- IInteractable ----------
    public void OnSelect()
    {
        // Aquí puedes poner highlight si quieres
    }

    public void OnDeselect()
    {
        // Quitar highlight si lo usas
    }

    public void Interact()
    {
        // Se llama cuando apuntas al slot y pulsas B

        // 1) Verificar que estemos en el paso correcto del checklist
        if (checklist != null && !string.IsNullOrEmpty(checklistStepName))
        {
            string current = checklist.GetCurrentItemName();
            if (current != checklistStepName)
            {
                Debug.Log($"[PadlockReceiver] Paso actual '{current}' no es '{checklistStepName}'.");
                return;
            }
        }

        // 2) Elegir el candado candidato:
        //    prioridad: padlockRef (si está asignado) → si no, el que esté en el trigger
        PadlockGrabber candidate = null;

        if (padlockRef != null)
            candidate = padlockRef;
        else
            candidate = padlockInTrigger;

        if (candidate == null)
        {
            Debug.Log("[PadlockReceiver] No hay candado asignado ni dentro del trigger.");
            return;
        }

        // 3) Debe estar en la mano y no ya bloqueado
        if (!candidate.IsHeld || candidate.IsLockedInPlace)
        {
            Debug.Log("[PadlockReceiver] El candado no está en la mano o ya está colocado.");
            return;
        }

        // 4) Encajamos el candado en el punto
        candidate.LockInPlace(snapPoint);

        // 5) Marcamos el paso en el procedimiento
        if (procedureManager != null)
            procedureManager.OnCandadoColocado();

        Debug.Log("[PadlockReceiver] Candado colocado correctamente.");

        // Ya no necesitamos la referencia del trigger
        if (candidate == padlockInTrigger)
            padlockInTrigger = null;
    }
    // -----------------------------------

    // ---------- Trigger para saber qué candado está cerca ----------
    private void OnTriggerEnter(Collider other)
    {
        var padlock = other.GetComponentInParent<PadlockGrabber>();
        if (padlock != null)
            padlockInTrigger = padlock;
    }

    private void OnTriggerExit(Collider other)
    {
        if (padlockInTrigger == null) return;

        if (other.transform.IsChildOf(padlockInTrigger.transform))
            padlockInTrigger = null;
    }
}
