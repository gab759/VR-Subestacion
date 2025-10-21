using UnityEngine;
using Meta.XR;

public class VRInteractor : MonoBehaviour
{
    [Header("Raycast Settings")]
    public Transform rayOrigin;
    public float raycastDistance = 3f;
    public LayerMask interactableLayerMask;
    private LineRenderer lineRenderer;

    [Header("Checklist")]
    public CheckList checklistManager;

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

        // Botón B para interactuar
        if (OVRInput.GetDown(OVRInput.Button.Two))
        {
            TryInteract();
        }

        // Botón A para mostrar/ocultar checklist
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            if (checklistManager != null)
                checklistManager.ToggleChecklist();
        }
    }

    void DetectInteractable()
    {
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
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

                if (showDebugRay)
                    Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green);
            }
            else
            {
                ClearCurrentInteractable();
                if (showDebugRay)
                    Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.yellow);
            }
        }
        else
        {
            lineRenderer.SetPosition(0, ray.origin);
            lineRenderer.SetPosition(1, ray.origin + ray.direction * raycastDistance);
            lineRenderer.startColor = Color.red;
            lineRenderer.endColor = Color.red;
            ClearCurrentInteractable();
            
            if (showDebugRay)
                Debug.DrawRay(ray.origin, ray.direction * raycastDistance, Color.red);
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