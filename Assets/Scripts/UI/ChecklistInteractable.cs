using UnityEngine;

public class ChecklistInteractable : MonoBehaviour, IInteractable
{
    [Header("Checklist Settings")]
    [SerializeField] private string itemName;
    [SerializeField] private CheckList checklistManager;
    
    [Header("Visual Feedback")]
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private GameObject completedEffect;
    
    private Renderer objectRenderer;
    private Color originalColor;
    private bool isCompleted = false;

    void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
            originalColor = objectRenderer.material.color;
            
        if (completedEffect != null)
            completedEffect.SetActive(false);
    }

    public void Interact()
    {
        if (isCompleted || checklistManager == null) return;

        // Verificar si este es el item actual en la checklist
        if (checklistManager.GetCurrentItemName() == itemName)
        {
            checklistManager.CompleteItem(itemName);
            isCompleted = true;
            
            if (completedEffect != null)
                completedEffect.SetActive(true);
                
            // Opcional: desactivar el objeto después de completarlo
            // gameObject.SetActive(false);
        }
        else
        {
            Debug.Log($"Primero debes completar: {checklistManager.GetCurrentItemName()}");
        }
    }

    public void OnSelect()
    {
        if (objectRenderer != null && !isCompleted)
            objectRenderer.material.color = highlightColor;
    }

    public void OnDeselect()
    {
        if (objectRenderer != null && !isCompleted)
            objectRenderer.material.color = originalColor;
    }
}