using UnityEngine;
using Meta.XR; // solo si estás usando el namespace de Meta SDK
// Si no te lo detecta, omítelo

public class VRInteractor : MonoBehaviour
{
    [Header("Raycast Settings")]
    public Transform rayOrigin; // asigna aquí la mano derecha o el controlador derecho
    public float raycastDistance = 3f;
    public LayerMask interactableLayerMask;
    private LineRenderer lineRenderer;

    [Header("Debug")]
    public bool showDebugRay = true;

    private IInteractable currentInteractable;
    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.startWidth = 0.005f;
        lineRenderer.endWidth = 0.002f;
        lineRenderer.positionCount = 2;
        lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
        lineRenderer.material.color = Color.yellow;
    }

    void Update()
    {
        DetectInteractable();

        // Verifica si se presionó el botón B del mando derecho
        if (OVRInput.GetDown(OVRInput.Button.Two)) // B en mando derecho
        {
            TryInteract();
        }
    }

    void DetectInteractable()
    {
        Ray ray = new Ray(rayOrigin.position, rayOrigin.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, raycastDistance, interactableLayerMask))
        {
            lineRenderer.SetPosition(0, ray.origin);
            lineRenderer.SetPosition(1, hit.point);
            lineRenderer.startColor = Color.green;
            lineRenderer.endColor = Color.green;

            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                if (currentInteractable != interactable)
                {
                    currentInteractable?.OnDeselect();
                    currentInteractable = interactable;
                    currentInteractable.OnSelect();
                }
            }
            else
            {
                ClearCurrentInteractable();
            }
        }
        else
        {
            lineRenderer.SetPosition(0, ray.origin);
            lineRenderer.SetPosition(1, ray.origin + ray.direction * raycastDistance);
            lineRenderer.startColor = Color.red;
            lineRenderer.endColor = Color.red;
            ClearCurrentInteractable();
        }
    }

    void TryInteract()
    {
        if (currentInteractable != null)
        {
            currentInteractable.Interact();
        }
    }

    void ClearCurrentInteractable()
    {
        if (currentInteractable != null)
        {
            currentInteractable.OnDeselect();
            currentInteractable = null;
        }
    }
}
