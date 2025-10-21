using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using Assets.Scripts.GameEvents;

public class ChecklistManager : MonoBehaviour
{
    [System.Serializable]
    public class ChecklistItem
    {
        public string itemId;
        public string description;
        public bool isCompleted;
        public int requiredOrder;
        [HideInInspector] public GameEventListener eventListener;
    }

    [Header("Checklist Settings")]
    public List<ChecklistItem> checklistItems = new List<ChecklistItem>();
    public GameEvent[] completionEvents;

    [Header("UI Reference")]
    public ChecklistUI checklistUI;

    [Header("Events")]
    public UnityEvent onChecklistComplete;

    private int currentStep = 0;
    private int completedItems = 0;

    void Start()
    {
        InitializeChecklist();
    }

    void InitializeChecklist()
    {
        // Ordenar items por orden requerido
        checklistItems.Sort((a, b) => a.requiredOrder.CompareTo(b.requiredOrder));

        // Configurar listeners de eventos
        for (int i = 0; i < checklistItems.Count; i++)
        {
            if (i < completionEvents.Length)
            {
                var listener = gameObject.AddComponent<GameEventListener>();
                listener.gameEvent = completionEvents[i];
                listener.response = new UnityEvent();
                
                int index = i; // Capturar índice para el closure
                listener.response.AddListener(() => CompleteItem(index));
                
                checklistItems[i].eventListener = listener;
            }
        }

        // Actualizar UI inicial
        if (checklistUI != null)
            checklistUI.UpdateChecklist(checklistItems, currentStep);
    }

    public void CompleteItem(int itemIndex)
    {
        if (itemIndex < 0 || itemIndex >= checklistItems.Count) return;
        
        var item = checklistItems[itemIndex];
        
        // Verificar orden correcto
        if (item.requiredOrder != currentStep)
        {
            Debug.LogWarning($"Orden incorrecto. Esperado: {currentStep}, Recibido: {item.requiredOrder}");
            return;
        }

        if (!item.isCompleted)
        {
            item.isCompleted = true;
            currentStep++;
            completedItems++;

            // Actualizar UI
            if (checklistUI != null)
                checklistUI.UpdateChecklist(checklistItems, currentStep);

            Debug.Log($"Item completado: {item.description}");

            // Verificar si se completó todo el checklist
            if (completedItems >= checklistItems.Count)
            {
                onChecklistComplete?.Invoke();
                Debug.Log("¡Checklist completo!");
            }
        }
    }

    public void CompleteItemById(string itemId)
    {
        int index = checklistItems.FindIndex(item => item.itemId == itemId);
        if (index != -1)
        {
            CompleteItem(index);
        }
    }

    public void ResetChecklist()
    {
        currentStep = 0;
        completedItems = 0;
        
        foreach (var item in checklistItems)
        {
            item.isCompleted = false;
        }

        if (checklistUI != null)
            checklistUI.UpdateChecklist(checklistItems, currentStep);
    }
}