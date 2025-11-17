using UnityEngine;
using System.Collections.Generic;

public class PadlockReceiver : MonoBehaviour, IInteractable
{
    [Header("Pose de colocación")]
    [Tooltip("Dónde se posicionará el candado. Si lo dejas vacío, usará este transform.")]
    public Transform snapPoint;

    [Header("Candado a aceptar")]
    [Tooltip("Candado específico (opcional). Si no se asigna, tomará el que esté dentro del trigger o el más cercano.")]
    public PadlockGrabber padlockRef;

    [Tooltip("Radio de búsqueda si no hay referencia ni trigger activo (opcional)")]
    public float searchRadius = 0.5f;

    [Header("Visual del receptor")]
    [Tooltip("Raíz visual a pintar (opcional). Si no se asigna, pinta los renderers de este objeto).")]
    public Transform visualRoot;

    public Color highlightColor = Color.yellow;

    [Header("Comportamiento al encajar")]
    [Tooltip("Desactiva el script PadlockGrabber del candado al encajar (recomendado).")]
    public bool disablePadlockScriptOnSnap = true;

    [Tooltip("Desparenta el candado al encajar (por si estaba parentado a la mano).")]
    public bool unparentOnSnap = true;

    [Tooltip("Alinear escala del candado con el snapPoint")]
    public bool matchScale = false;

    // Internos visual
    private Renderer[] renderers;
    private Color[] originalColors;
    private bool isHighlighted;

    // Candidato detectado por trigger
    private PadlockGrabber lastPadlockInTrigger;

    void Awake()
    {
        if (snapPoint == null) snapPoint = transform;

        Transform vis = visualRoot != null ? visualRoot : transform;
        renderers = vis.GetComponentsInChildren<Renderer>(true);

        var cols = new List<Color>(renderers.Length);
        foreach (var r in renderers) cols.Add(GetColor(r));
        originalColors = cols.ToArray();
    }

    // ---------- IInteractable ----------
    public void OnSelect() { SetHighlight(true); }
    public void OnDeselect() { SetHighlight(false); }

    public void Interact()
    {
        // B sobre el receptor → encajar
        PadlockGrabber padlock = ResolvePadlockCandidate();
        if (padlock == null)
        {
            Debug.Log("[PadlockReceiver] No encontré candado (asigna padlockRef o pon el candado dentro del trigger).");
            return;
        }

        SnapPadlock(padlock);
    }
    // -----------------------------------

    private PadlockGrabber ResolvePadlockCandidate()
    {
        if (padlockRef != null) return padlockRef;
        if (lastPadlockInTrigger != null) return lastPadlockInTrigger;

        // Fallback: buscar por proximidad un PadlockGrabber
        Collider[] cols = Physics.OverlapSphere(snapPoint.position, searchRadius, ~0, QueryTriggerInteraction.Collide);
        float best = float.MaxValue;
        PadlockGrabber bestPadlock = null;

        foreach (var c in cols)
        {
            var p = c.GetComponentInParent<PadlockGrabber>();
            if (p != null)
            {
                float d = (p.transform.position - snapPoint.position).sqrMagnitude;
                if (d < best) { best = d; bestPadlock = p; }
            }
        }
        return bestPadlock;
    }

    private void SnapPadlock(PadlockGrabber padlock)
    {
        Transform t = padlock.transform;

        if (unparentOnSnap) t.SetParent(null, true);

        t.position = snapPoint.position;
        t.rotation = snapPoint.rotation;
        if (matchScale) t.localScale = snapPoint.lossyScale;

        if (disablePadlockScriptOnSnap)
            padlock.enabled = false;

        // quitar highlight del receptor
        SetHighlight(false, true);
    }

    // ---------- Trigger para detectar el candado ----------
    void OnTriggerEnter(Collider other)
    {
        var p = other.GetComponentInParent<PadlockGrabber>();
        if (p != null) lastPadlockInTrigger = p;
    }

    void OnTriggerExit(Collider other)
    {
        if (lastPadlockInTrigger == null) return;
        if (other.transform.IsChildOf(lastPadlockInTrigger.transform))
            lastPadlockInTrigger = null;
    }
    // ------------------------------------------------------

    // ---------- Helpers de highlight (URP/Standard) ----------
    private void SetHighlight(bool on, bool forceOriginal = false)
    {
        if (renderers == null) return;

        if (forceOriginal)
        {
            for (int i = 0; i < renderers.Length; i++) SetColor(renderers[i], originalColors[i]);
            isHighlighted = false; return;
        }

        if (on && !isHighlighted)
        {
            for (int i = 0; i < renderers.Length; i++) SetColor(renderers[i], highlightColor);
            isHighlighted = true;
        }
        else if (!on && isHighlighted)
        {
            for (int i = 0; i < renderers.Length; i++) SetColor(renderers[i], originalColors[i]);
            isHighlighted = false;
        }
    }

    private static Color GetColor(Renderer r)
    {
        var m = r.material;
        if (m.HasProperty("_BaseColor")) return m.GetColor("_BaseColor");
        if (m.HasProperty("_Color")) return m.GetColor("_Color");
        return Color.white;
    }
    private static void SetColor(Renderer r, Color c)
    {
        var m = r.material;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        else if (m.HasProperty("_Color")) m.SetColor("_Color", c);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Transform sp = snapPoint != null ? snapPoint : transform;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(sp.position, Vector3.one * 0.05f);
        Gizmos.DrawLine(sp.position, sp.position + sp.forward * 0.1f);
    }
#endif
}
