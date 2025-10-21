using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;

public class ChecklistUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Transform checklistContainer;
    public GameObject checklistItemPrefab;
    public Text titleText;
    public Color completedColor = Color.green;
    public Color currentColor = Color.yellow;
    public Color pendingColor = Color.white;

    [Header("Checklist Sections")]
    public string[] sectionTitles = {
        "Paso 1: Equipo de Protección Personal",
        "Paso 2: Guantes de Protección"
    };

    public void UpdateChecklist(List<ChecklistManager.ChecklistItem> items, int currentStep)
    {
        // Limpiar contenedor
        foreach (Transform child in checklistContainer)
        {
            Destroy(child.gameObject);
        }

        StringBuilder currentSection = new StringBuilder();
        int lastSection = -1;

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            
            // Determinar sección actual
            int section = GetSectionIndex(item.requiredOrder);
            if (section != lastSection)
            {
                AddSectionTitle(sectionTitles[section]);
                lastSection = section;
            }

            // Crear item de UI
            GameObject itemUI = Instantiate(checklistItemPrefab, checklistContainer);
            ChecklistItemUI itemComponent = itemUI.GetComponent<ChecklistItemUI>();
            
            if (itemComponent != null)
            {
                // Determinar estado y color
                ChecklistItemUI.ItemState state;
                Color color;
                
                if (item.isCompleted)
                {
                    state = ChecklistItemUI.ItemState.Completed;
                    color = completedColor;
                }
                else if (item.requiredOrder == currentStep)
                {
                    state = ChecklistItemUI.ItemState.Current;
                    color = currentColor;
                }
                else
                {
                    state = ChecklistItemUI.ItemState.Pending;
                    color = pendingColor;
                }

                itemComponent.SetupItem(item.description, state, color);
            }
        }
    }

    private int GetSectionIndex(int order)
    {
        // Definir rangos de órdenes para cada sección
        if (order <= 4) return 0; // Equipo de protección personal
        else return 1; // Guantes
    }

    private void AddSectionTitle(string title)
    {
        GameObject titleObject = new GameObject("SectionTitle");
        titleObject.transform.SetParent(checklistContainer, false);
        
        Text titleTextComponent = titleObject.AddComponent<Text>();
        titleTextComponent.text = title;
        // Usar la fuente por defecto de Unity (LegacyRuntime) o una fuente del proyecto
        titleTextComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleTextComponent.fontSize = 18;
        titleTextComponent.fontStyle = FontStyle.Bold;
        titleTextComponent.color = Color.cyan;
        titleTextComponent.alignment = TextAnchor.MiddleLeft;
        
        RectTransform rect = titleObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300, 35);
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        
        // Agregar padding
        LayoutElement layout = titleObject.AddComponent<LayoutElement>();
        layout.minHeight = 35;
        layout.preferredHeight = 35;
    }
}