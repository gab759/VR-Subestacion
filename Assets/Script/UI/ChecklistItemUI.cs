using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChecklistItemUI : MonoBehaviour
{
    public enum ItemState { Completed, Current, Pending }

    [Header("UI Elements")]
    public TextMeshProUGUI itemText;
    public Image checkmark;
    public Image background;

    public void SetupItem(string description, ItemState state, Color color)
    {
        if (itemText != null)
        {
            itemText.text = description;
            itemText.color = color;
        }

        if (checkmark != null)
        {
            checkmark.gameObject.SetActive(state == ItemState.Completed);
        }

        // Opcional: cambiar fondo para el item actual
        if (background != null && state == ItemState.Current)
        {
            background.color = new Color(color.r, color.g, color.b, 0.3f);
        }
    }
}