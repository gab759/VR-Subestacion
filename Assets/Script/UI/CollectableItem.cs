using UnityEngine;
using Assets.Scripts.GameEvents;
public class CollectableItem : MonoBehaviour, IInteractable
{
    [Header("Checklist Settings")]
    public string itemId;
    public GameEvent completionEvent;

    [Header("Visual Feedback")]
    public Color highlightColor = Color.yellow;
    
    private Renderer itemRenderer;
    private Color originalColor;
    private bool isCollected = false;

    void Start()
    {
        itemRenderer = GetComponent<Renderer>();
        if (itemRenderer != null)
            originalColor = itemRenderer.material.color;
    }

    public void Interact()
    {
        if (!isCollected)
        {
            CollectItem();
        }
    }

    private void CollectItem()
    {
        isCollected = true;
        
        // Disparar evento de completado
        if (completionEvent != null)
            completionEvent.Raise();
        
        // Opcional: desactivar o hacer invisible el objeto
        gameObject.SetActive(false);
        
        Debug.Log($"Item recolectado: {itemId}");
    }

    public void OnSelect()
    {
        if (!isCollected && itemRenderer != null)
            itemRenderer.material.color = highlightColor;
    }

    public void OnDeselect()
    {
        if (!isCollected && itemRenderer != null)
            itemRenderer.material.color = originalColor;
    }
}