using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using Assets.Scripts.GameEvents;

[System.Serializable]
public class ChecklistItem
{
    public string itemName;
    public int orderIndex;
    public bool isCompleted = false;
    [HideInInspector] public GameObject itemUI;
    [HideInInspector] public Image checkmarkImage;
    [HideInInspector] public TextMeshProUGUI itemText;
}

public class CheckList : MonoBehaviour
{
    [Header("Checklist Items")]
    [SerializeField] private List<ChecklistItem> checklistItems = new List<ChecklistItem>();

    [Header("UI References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject checklistPanel;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private Transform itemContainer;
    
    [Header("VR Settings")]
    [SerializeField] private bool isVRMode = true;
    [SerializeField] private Transform vrCamera;
    [SerializeField] private Vector3 vrOffset = new Vector3(0.5f, 0, 0.8f);
    [SerializeField] private bool followPlayer = true;
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private bool alwaysVisible = true;
    [SerializeField] private float canvasScale = 0.001f;
    
    [Header("Colors")]
    [SerializeField] private Color completedColor = Color.green;
    [SerializeField] private Color pendingColor = Color.gray;
    [SerializeField] private Color currentColor = Color.yellow;
    
    [Header("Events")]
    [SerializeField] private GameEvent onChecklistCompleted;
    [SerializeField] private GameIntEvent onItemCompleted;
    
    [Header("Audio (Optional)")]
    [SerializeField] private AudioClip completeSound;
    [SerializeField] private AudioClip checklistCompleteSound;
    private AudioSource audioSource;
    
    private int currentItemIndex = 0;
    private bool isChecklistComplete = false;

    void Start()
    {
        InitializeVRCanvas();
        InitializeChecklist();
        UpdateUI();
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (completeSound != null || checklistCompleteSound != null))
            audioSource = gameObject.AddComponent<AudioSource>();

        Debug.Log($"Canvas encontrado: {canvas != null}");
        Debug.Log($"Canvas RenderMode: {canvas?.renderMode}");
        Debug.Log($"VR Camera: {vrCamera?.name}");
        Debug.Log($"ChecklistPanel activo: {checklistPanel?.activeSelf}");
        Debug.Log($"Items en checklist: {checklistItems.Count}");
    }

    void Update()
    {
        if (isVRMode && followPlayer && vrCamera != null && checklistPanel != null)
        {
            UpdateVRPosition();
        }
    }

    private void InitializeVRCanvas()
    {
        // Buscar el canvas si no está asignado
        if (canvas == null)
            canvas = GetComponentInChildren<Canvas>();
            
        if (canvas == null)
        {
            Debug.LogError("No se encontró Canvas. Asegúrate de tener un Canvas como hijo de ChecklistManager y asignarlo en el Inspector.");
            return;
        }

        if (isVRMode)
        {
            // Configurar Canvas para VR
            canvas.renderMode = RenderMode.WorldSpace;
            
            // Buscar VR Camera si no está asignada
            if (vrCamera == null)
            {
                GameObject cameraRig = GameObject.Find("OVRCameraRig");
                if (cameraRig != null)
                {
                    Transform centerEye = cameraRig.transform.Find("TrackingSpace/CenterEyeAnchor");
                    if (centerEye != null)
                        vrCamera = centerEye;
                }
                
                if (vrCamera == null)
                    vrCamera = Camera.main?.transform;
            }
            
            // Asignar cámara al Canvas
            canvas.worldCamera = vrCamera != null ? vrCamera.GetComponent<Camera>() : Camera.main;
            
            // Configurar el tamaño del Canvas
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                canvasRect.sizeDelta = new Vector2(1000, 1500);
            }
            
            // Escalar el Canvas para VR
            canvas.transform.localScale = Vector3.one * canvasScale;
            
            // Posicionar inicialmente
            if (vrCamera != null)
            {
                UpdateVRPosition();
            }
        }
        else
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        // Mostrar u ocultar panel según configuración
        if (checklistPanel != null)
            checklistPanel.SetActive(alwaysVisible);
    }

    private void UpdateVRPosition()
    {
        if (vrCamera == null) return;
        
        if (followPlayer)
        {
            // Calcular posición frente al jugador
            Vector3 targetPosition = vrCamera.position + 
                                     vrCamera.right * vrOffset.x + 
                                     vrCamera.up * vrOffset.y + 
                                     vrCamera.forward * vrOffset.z;
            
            // Mover suavemente hacia la posición objetivo
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);
            
            // Rotar para mirar al jugador
            Vector3 directionToCamera = vrCamera.position - transform.position;
            directionToCamera.y = 0; // Mantener horizontal
            
            if (directionToCamera != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(-directionToCamera);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * followSpeed);
            }
        }
    }

    private void InitializeChecklist()
    {
        // Ordenar items por índice
        checklistItems.Sort((a, b) => a.orderIndex.CompareTo(b.orderIndex));
        
        if (itemContainer == null || itemPrefab == null)
        {
            Debug.LogError("ItemContainer o ItemPrefab no están asignados en el Inspector.");
            return;
        }

        // Limpiar items existentes en el container
        foreach (Transform child in itemContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Crear UI para cada item
        foreach (var item in checklistItems)
        {
            GameObject itemObj = Instantiate(itemPrefab, itemContainer);
            item.itemUI = itemObj;
            
            // Buscar componentes del item
            item.checkmarkImage = itemObj.transform.Find("Checkmark")?.GetComponent<Image>();
            item.itemText = itemObj.GetComponentInChildren<TextMeshProUGUI>();
            
            // Si no encuentra con Find, buscar en hijos directos
            if (item.itemText == null)
            {
                TextMeshProUGUI[] texts = itemObj.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length > 0)
                    item.itemText = texts[0];
            }
            
            if (item.itemText != null)
            {
                item.itemText.text = item.itemName;
                if (isVRMode)
                    item.itemText.fontSize = 36;
            }
            else
            {
                Debug.LogWarning($"No se encontró TextMeshProUGUI en el item: {item.itemName}");
            }
            
            if (item.checkmarkImage != null)
            {
                item.checkmarkImage.enabled = false;
            }
            else
            {
                Debug.LogWarning($"No se encontró Checkmark Image en el item: {item.itemName}");
            }
        }
        
        Debug.Log($"Checklist inicializada con {checklistItems.Count} items");
    }

    public void CompleteItem(string itemName)
    {
        if (isChecklistComplete || currentItemIndex >= checklistItems.Count)
        {
            Debug.LogWarning("Checklist ya completada o índice fuera de rango");
            return;
        }

        ChecklistItem currentItem = checklistItems[currentItemIndex];
        
        if (currentItem.itemName == itemName && !currentItem.isCompleted)
        {
            currentItem.isCompleted = true;
            onItemCompleted?.Raise(currentItemIndex);
            
            if (audioSource != null && completeSound != null)
                audioSource.PlayOneShot(completeSound);
            
            Debug.Log($"✓ Completado: {currentItem.itemName} ({currentItemIndex + 1}/{checklistItems.Count})");
            
            currentItemIndex++;
            
            if (currentItemIndex >= checklistItems.Count)
            {
                isChecklistComplete = true;
                onChecklistCompleted?.Raise();
                
                if (audioSource != null && checklistCompleteSound != null)
                    audioSource.PlayOneShot(checklistCompleteSound);
                
                Debug.Log("🎉 ¡Checklist completada!");
            }
            
            UpdateUI();
        }
        else
        {
            Debug.LogWarning($"⚠ Debes completar '{currentItem.itemName}' antes de '{itemName}'");
        }
    }

    public void CompleteCurrentItem()
    {
        if (currentItemIndex < checklistItems.Count)
        {
            CompleteItem(checklistItems[currentItemIndex].itemName);
        }
    }

    private void UpdateUI()
    {
        for (int i = 0; i < checklistItems.Count; i++)
        {
            ChecklistItem item = checklistItems[i];
            
            if (item.checkmarkImage != null)
            {
                if (item.isCompleted)
                {
                    item.checkmarkImage.color = completedColor;
                    item.checkmarkImage.enabled = true;
                }
                else if (i == currentItemIndex)
                {
                    item.checkmarkImage.color = currentColor;
                    item.checkmarkImage.enabled = true;
                }
                else
                {
                    item.checkmarkImage.color = pendingColor;
                    item.checkmarkImage.enabled = false;
                }
            }
            
            if (item.itemText != null)
            {
                if (item.isCompleted)
                {
                    item.itemText.fontStyle = FontStyles.Strikethrough;
                    item.itemText.color = completedColor;
                }
                else if (i == currentItemIndex)
                {
                    item.itemText.fontStyle = FontStyles.Bold;
                    item.itemText.color = currentColor;
                }
                else
                {
                    item.itemText.fontStyle = FontStyles.Normal;
                    item.itemText.color = pendingColor;
                }
            }
        }
    }

    public void ToggleChecklist()
    {
        if (checklistPanel != null)
        {
            bool newState = !checklistPanel.activeSelf;
            checklistPanel.SetActive(newState);
            Debug.Log($"Checklist {(newState ? "mostrado" : "ocultado")}");
        }
    }

    public void ShowChecklist(bool show)
    {
        if (checklistPanel != null)
            checklistPanel.SetActive(show);
    }

    public bool IsItemCompleted(string itemName)
    {
        foreach (var item in checklistItems)
        {
            if (item.itemName == itemName)
                return item.isCompleted;
        }
        return false;
    }

    public string GetCurrentItemName()
    {
        if (currentItemIndex < checklistItems.Count)
            return checklistItems[currentItemIndex].itemName;
        return "";
    }

    public int GetCompletedCount()
    {
        int count = 0;
        foreach (var item in checklistItems)
        {
            if (item.isCompleted) count++;
        }
        return count;
    }

    public float GetCompletionPercentage()
    {
        if (checklistItems.Count == 0) return 0f;
        return (float)GetCompletedCount() / checklistItems.Count * 100f;
    }

    public void OnVRButtonPressed()
    {
        ToggleChecklist();
    }

    // Método para agregar items desde el Inspector o código
    public void AddChecklistItem(string itemName, int orderIndex)
    {
        ChecklistItem newItem = new ChecklistItem
        {
            itemName = itemName,
            orderIndex = orderIndex,
            isCompleted = false
        };
        checklistItems.Add(newItem);
    }

    // Para debug en el editor
    void OnDrawGizmos()
    {
        if (vrCamera != null && isVRMode)
        {
            // Dibujar línea desde la cámara hasta donde debería estar el checklist
            Vector3 targetPos = vrCamera.position + 
                               vrCamera.right * vrOffset.x + 
                               vrCamera.up * vrOffset.y + 
                               vrCamera.forward * vrOffset.z;
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(vrCamera.position, targetPos);
            Gizmos.DrawWireSphere(targetPos, 0.1f);
        }
    }
}