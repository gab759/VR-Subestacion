using UnityEngine;
using Meta.XR; // solo si estás usando el namespace de Meta SDK
// Si no te lo detecta, omítelo

public class VRInteractor : MonoBehaviour
{
    [Header("Raycast Settings")]
    public Transform rayOrigin; // asigna aquí la mano derecha o el controlador derecho
    public float raycastDistance = 3f;
    public LayerMask interactableLayerMask;

    [Header("Debug")]
    public bool showDebugRay = true;

    private IInteractable currentInteractable;

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
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, raycastDistance, interactableLayerMask))
        {
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
