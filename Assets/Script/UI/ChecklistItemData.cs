using UnityEngine;

[CreateAssetMenu(fileName = "ChecklistItemData", menuName = "Scriptable Objects/ChecklistItemData")]
public class ChecklistItemData : ScriptableObject
{
    public string itemName;
    public int orderIndex;
    public string description;
}
