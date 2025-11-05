using UnityEngine;

public class DetectorTarget : MonoBehaviour, IInteractable
{
    [Header("Refs")]
    [Tooltip("Transform destino exacto (puede ser este mismo transform)")]
    public Transform placementPoint;

    [Header("Feedback (opcional)")]
    public Color highlightColor = Color.cyan;

    private Renderer[] renderers;
    private Color[] originalColors;

    void Awake()
    {
        if (placementPoint == null) placementPoint = transform;

        renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers != null)
        {
            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                var m = renderers[i].material;
                if (m.HasProperty("_BaseColor")) originalColors[i] = m.GetColor("_BaseColor");
                else if (m.HasProperty("_Color")) originalColors[i] = m.GetColor("_Color");
                else originalColors[i] = Color.white;
            }
        }
    }

    // ============ IInteractable ============
    public void OnSelect()
    {
        SetHighlight(true);
    }

    public void OnDeselect()
    {
        SetHighlight(false);
    }

    public void Interact()
    {
        var held = DetectorBGrabber.CurrentRightHandHeld;
        if (held != null)
        {
            held.PlaceOn(placementPoint);
        }
        // Si no hay nada en mano, no hace nada.
    }
    // ======================================

    private void SetHighlight(bool on)
    {
        if (renderers == null || originalColors == null) return;

        if (on)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                var m = renderers[i].material;
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", highlightColor);
                else if (m.HasProperty("_Color")) m.SetColor("_Color", highlightColor);
            }
        }
        else
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                var m = renderers[i].material;
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", originalColors[i]);
                else if (m.HasProperty("_Color")) m.SetColor("_Color", originalColors[i]);
            }
        }
    }
}