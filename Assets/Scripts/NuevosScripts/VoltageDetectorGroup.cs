using UnityEngine;

public class VoltageDetectorGroup : MonoBehaviour
{
    [Header("Targets de este grupo")]
    [SerializeField] private VoltageDetectorTarget[] targets;

    [Header("Detector")]
    [Tooltip("Tag del collider de la punta del detector.")]
    [SerializeField] private string detectorTag = "Detector";

    [Header("Checklist / Procedimiento")]
    [SerializeField] private CheckList checklist;
    [SerializeField] private SubstationProcedureManager procedureManager;
    [Tooltip("Nombre del paso que este grupo completa (ej: 'VerificarBarrasPuerta1_Abajo').")]
    [SerializeField] private string checklistStepName;

    [Header("Hint (parpadeo si no hace nada)")]
    [SerializeField] private float hintDelay = 10f;
    [SerializeField] private float blinkSpeed = 4f;
    [SerializeField] private Color hintColor = Color.yellow;

    private float hintTimer = 0f;
    private bool isBlinking = false;

    private void Update()
    {
        if (checklist == null || string.IsNullOrEmpty(checklistStepName) || targets == null)
            return;

        bool stepActive = IsStepActive();

        if (stepActive && !AreAllTargetsChecked())
        {
            hintTimer += Time.deltaTime;

            if (hintTimer >= hintDelay)
            {
                isBlinking = true;
                bool on = Mathf.FloorToInt(Time.time * blinkSpeed) % 2 == 0;

                foreach (var t in targets)
                {
                    if (t == null || t.IsChecked) continue;
                    t.SetBlinkColor(on ? hintColor : Color.white);
                }
            }
        }
        else
        {
            hintTimer = 0f;

            if (isBlinking)
            {
                isBlinking = false;
                foreach (var t in targets)
                {
                    if (t == null || t.IsChecked) continue;
                    t.ResetIdleColor();
                }
            }
        }
    }

    // 👉 NUEVO: este grupo solo está “activo” si su paso es el actual y no se ha completado
    public bool IsStepActive()
    {
        if (checklist == null || string.IsNullOrEmpty(checklistStepName))
            return false;

        string current = checklist.GetCurrentItemName();
        bool stepCompleted = checklist.IsItemCompleted(checklistStepName);

        return (current == checklistStepName) && !stepCompleted;
    }

    public bool IsDetectorCollider(Collider other)
    {
        return string.IsNullOrEmpty(detectorTag) || other.CompareTag(detectorTag);
    }

    public void OnTargetHit(VoltageDetectorTarget target)
    {
        // IMPORTANTÍSIMO: si este grupo no es el paso actual, no hacemos nada
        if (!IsStepActive())
            return;

        if (!AreAllTargetsChecked())
            return;

        if (procedureManager != null && !string.IsNullOrEmpty(checklistStepName))
        {
            procedureManager.EvaluateStep(checklistStepName);
            Debug.Log($"[VoltageDetectorGroup] Paso completado: {checklistStepName}");
        }
    }

    private bool AreAllTargetsChecked()
    {
        bool any = false;
        foreach (var t in targets)
        {
            if (t == null) continue;
            any = true;
            if (!t.IsChecked) return false;
        }
        return any; // solo true si había targets y todos están marcados
    }
}
