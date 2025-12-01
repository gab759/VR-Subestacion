using UnityEngine;

public class PadlockGrabber : MonoBehaviour, IInteractable
{
    [Header("Referencia de la mano")]
    [Tooltip("Transform de la mano donde se sujetará el candado (ej: RightHandAnchor).")]
    [SerializeField] private Transform handAnchor;

    [Header("Checklist (opcional para bloquear el pickup)")]
    [SerializeField] private CheckList checklist;
    [Tooltip("Pasos en los que se permite AGARRAR este candado (ej: solo 'ColocarCandado').")]
    [SerializeField] private string[] allowedSteps;

    [Header("Visual")]
    [SerializeField] private Color highlightColor = Color.yellow;

    private Transform originalParent;
    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;

    private Renderer rend;
    private Color originalColor;

    private Rigidbody rb;
    private Collider col;

    private bool isHeld = false;
    private bool isLockedInPlace = false;

    public bool IsHeld => isHeld;
    public bool IsLockedInPlace => isLockedInPlace;

    private void Awake()
    {
        originalParent = transform.parent;
        originalLocalPos = transform.localPosition;
        originalLocalRot = transform.localRotation;

        rend = GetComponentInChildren<Renderer>();
        if (rend != null)
            originalColor = rend.material.color;

        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    public void Interact()
    {
        // Si ya está encajado en el receptor, no se puede volver a manipular
        if (isLockedInPlace)
            return;

        // Bloqueo por checklist (opcional)
        if (checklist != null && allowedSteps != null && allowedSteps.Length > 0)
        {
            string current = checklist.GetCurrentItemName();
            bool allowed = false;

            foreach (var step in allowedSteps)
            {
                if (!string.IsNullOrEmpty(step) && current == step)
                {
                    allowed = true;
                    break;
                }
            }

            if (!allowed)
                return;
        }

        if (!isHeld)
        {
            PickUp();
        }
        else
        {
            DropToOriginal();
        }
    }

    private void PickUp()
    {
        if (handAnchor == null)
        {
            Debug.LogWarning("[PadlockGrabber] No se asignó handAnchor.");
            return;
        }

        isHeld = true;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (col != null)
            col.isTrigger = true; // para que no choque raro al acercarlo al receptor

        transform.SetParent(handAnchor);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    private void DropToOriginal()
    {
        isHeld = false;

        transform.SetParent(originalParent);
        transform.localPosition = originalLocalPos;
        transform.localRotation = originalLocalRot;

        if (rb != null)
            rb.isKinematic = false;

        if (col != null)
            col.isTrigger = false;
    }

    /// <summary>
    /// Llamado desde el PadlockReceiver cuando el candado se encaja en el medio de los protectores.
    /// </summary>
    public void LockInPlace(Transform snapPoint)
    {
        isHeld = false;
        isLockedInPlace = true;

        transform.SetParent(snapPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        if (rb != null)
            rb.isKinematic = true;

        if (col != null)
            col.enabled = false; // ya no se usa como objeto físico

        // opcional: quitar highlight
        if (rend != null)
            rend.material.color = originalColor;
    }

    public void OnSelect()
    {
        if (rend != null && !isLockedInPlace)
            rend.material.color = highlightColor;
    }

    public void OnDeselect()
    {
        if (rend != null && !isLockedInPlace)
            rend.material.color = originalColor;
    }
}
