using UnityEngine;

public class VoltageDetectorTarget : MonoBehaviour
{
    [SerializeField] private VoltageDetectorGroup group;

    [Header("Colores")]
    [SerializeField] private Renderer rend;
    [SerializeField] private Color idleColor = Color.gray;
    [SerializeField] private Color hitColor = Color.yellow;

    [Header("Tiempo mínimo de contacto")]
    [Tooltip("Tiempo que el detector debe permanecer sobre la barra para marcarla como verificada.")]
    [SerializeField] private float requiredStayTime = 1f;

    private bool isChecked = false;
    public bool IsChecked => isChecked;

    // control de permanencia
    private bool detectorInside = false;
    private float stayTimer = 0f;

    private void Awake()
    {
        if (rend == null)
            rend = GetComponent<Renderer>();

        if (rend != null)
            rend.material.color = idleColor;
    }

    private void Update()
    {
        if (isChecked || !detectorInside || group == null)
            return;

        // 👉 solo contamos tiempo si este grupo está en el paso actual
        if (!group.IsStepActive())
            return;

        stayTimer += Time.deltaTime;

        if (stayTimer >= requiredStayTime)
        {
            MarkAsChecked();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isChecked || group == null)
            return;

        // 👉 si este grupo NO es el paso actual, ignoramos el detector
        if (!group.IsStepActive())
            return;

        if (!group.IsDetectorCollider(other))
            return;

        detectorInside = true;
        stayTimer = 0f; // empezamos a contar desde que entra

        // al tocar, se ilumina
        if (rend != null)
            rend.material.color = hitColor;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!detectorInside || group == null)
            return;

        if (!group.IsDetectorCollider(other))
            return;

        // salió antes de cumplir el tiempo → reset
        detectorInside = false;
        stayTimer = 0f;

        // si aún no se había verificado, vuelve al color idle
        if (!isChecked && rend != null)
            rend.material.color = idleColor;
    }

    private void MarkAsChecked()
    {
        isChecked = true;
        detectorInside = false;
        stayTimer = 0f;

        // ✅ después del segundo requerido, vuelve a su color original
        if (rend != null)
            rend.material.color = idleColor;

        group.OnTargetHit(this);
    }

    // usados por el hint del grupo
    public void SetBlinkColor(Color c)
    {
        if (isChecked || rend == null) return;
        rend.material.color = c;
    }

    public void ResetIdleColor()
    {
        if (isChecked || rend == null) return;
        rend.material.color = idleColor;
    }
}
