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
    public GameObject itemUI;
    public Image checkmarkImage;
    public TextMeshProUGUI itemText;
}

public class CheckList : MonoBehaviour
{
    [Header("Checklist Items")]
    [SerializeField] private List<ChecklistItem> checklistItems = new List<ChecklistItem>();
    
    [Header("UI References")]
    [SerializeField] private GameObject checklistPanel;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private Transform itemContainer;
    
    [Header("VR Settings")]
    [SerializeField] private bool isVRMode = true;
    [SerializeField] private Transform vrCamera;
    [SerializeField] private Vector3 vrOffset = new Vector3(0.3f, 0, 0.5f);
    [SerializeField] private bool followPlayer = true;
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private bool alwaysVisible = true;
    
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
    private Canvas canvas;

    void Start()
    {
        InitializeVRCanvas();
        InitializeChecklist();
        UpdateUI();
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (completeSound != null || checklistCompleteSound != null))
            audioSource = gameObject.AddComponent<AudioSource>();
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
        canvas = GetComponentInChildren<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("No se encontró Canvas. Asegúrate de tener un Canvas en el checklist.");
            return;
        }

        if (isVRMode)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = vrCamera != null ? vrCamera.GetComponent<Camera>() : Camera.main;
            transform.localScale = Vector3.one * 0.001f;
            
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
                    vrCamera = Camera.main.transform;
            }
        }
        else
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        if (checklistPanel != null)
            checklistPanel.SetActive(alwaysVisible);
    }

    private void UpdateVRPosition()
    {
        if (followPlayer)
        {
            Vector3 targetPosition = vrCamera.position + 
                                     vrCamera.right * vrOffset.x + 
                                     vrCamera.up * vrOffset.y + 
                                     vrCamera.forward * vrOffset.z;
            
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);
            
            Vector3 directionToCamera = vrCamera.position - transform.position;
            directionToCamera.y = 0;
            if (directionToCamera != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(-directionToCamera);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * followSpeed);
            }
        }
    }

    private void InitializeChecklist()
    {
        checklistItems.Sort((a, b) => a.orderIndex.CompareTo(b.orderIndex));
        
        if (itemContainer != null && itemPrefab != null)
        {
            foreach (var item in checklistItems)
            {
                if (item.itemUI == null)
                {
                    GameObject itemObj = Instantiate(itemPrefab, itemContainer);
                    item.itemUI = itemObj;
                    item.checkmarkImage = itemObj.transform.Find("Checkmark")?.GetComponent<Image>();
                    item.itemText = itemObj.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
                    
                    if (item.itemText != null)
                    {
                        item.itemText.text = item.itemName;
                        if (isVRMode)
                            item.itemText.fontSize = 48;
                    }
                }
            }
        }
    }

    public void CompleteItem(string itemName)
    {
        if (isChecklistComplete || currentItemIndex >= checklistItems.Count)
            return;

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
                
                Debug.Log("¡Checklist completada!");
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
            checklistPanel.SetActive(!checklistPanel.activeSelf);
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
}