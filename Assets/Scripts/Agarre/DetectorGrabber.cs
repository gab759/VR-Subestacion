using System.Collections.Generic;
using UnityEngine;
public class DetectorGrabber : MonoBehaviour, IInteractable
{
    [Header("Refs")]
    [Tooltip("Transform del controlador/mano DERECHA (tu rayOrigin del mando derecho)")]
    public Transform rightHandAnchor;

    [Header("Follow (sin parent)")]
    public Vector3 localPositionOffset = Vector3.zero;
    public Vector3 localEulerOffset = Vector3.zero;
    [Range(0f, 40f)] public float followSmoothing = 20f;

    [Header("Snap (seguridad)")]
    [Tooltip("Distancia máxima para permitir encajar si el placement está cerca")]
    public float snapDistance = 0.12f;

    [Header("Highlight (opcional)")]
    public Color highlightColor = Color.yellow;

    // Estado global simple (una mano derecha)
    public static DetectorGrabber CurrentRightHandHeld { get; private set; }

    // Internos
    private bool isFollowing = false;
    private Transform _tr;
    private Renderer[] renderers;
    private Color[] originalColors;
    private Rigidbody rb;
    private bool rbHadKinematic;
    private bool rbHadUseGravity;

    void Awake()
    {
        _tr = transform;

        renderers = GetComponentsInChildren<Renderer>(true);
        var cols = new List<Color>(renderers.Length);
        foreach (var r in renderers) cols.Add(GetColor(r));
        originalColors = cols.ToArray();

        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rbHadKinematic = rb.isKinematic;
            rbHadUseGravity = rb.useGravity;
        }
    }

    void LateUpdate()
    {
        if (!isFollowing || rightHandAnchor == null) return;

        Vector3 targetPos = rightHandAnchor.TransformPoint(localPositionOffset);
        Quaternion targetRot = rightHandAnchor.rotation * Quaternion.Euler(localEulerOffset);

        if (followSmoothing > 0f)
        {
            float k = 1f - Mathf.Exp(-followSmoothing * Time.deltaTime);
            _tr.position = Vector3.Lerp(_tr.position, targetPos, k);
            _tr.rotation = Quaternion.Slerp(_tr.rotation, targetRot, k);
        }
        else
        {
            _tr.position = targetPos;
            _tr.rotation = targetRot;
        }
    }

    // ============ IInteractable ============
    public void OnSelect()
    {
        if (isFollowing) return;
        SetHighlight(true);
    }

    public void OnDeselect()
    {
        if (isFollowing) return;
        SetHighlight(false);
    }

    public void Interact()
    {
        // Si no está en mano → tomar
        if (!isFollowing)
        {
            TryStartFollowing();
            return;
        }

        // Si ya está en mano y se vuelve a pulsar sobre el MISMO objeto,
        // puedes permitir "drop al aire" si lo deseas:
        // StopFollowingHand(restorePhysics: true);
    }
    // ======================================

    private void TryStartFollowing()
    {
        if (rightHandAnchor == null)
        {
            Debug.LogWarning("[GrabPlaceable] Falta rightHandAnchor.");
            return;
        }

        // Si ya hay otro en mano derecha, lo soltamos primero (opcional)
        if (CurrentRightHandHeld != null && CurrentRightHandHeld != this)
        {
            CurrentRightHandHeld.StopFollowingHand(true);
        }

        SetHighlight(false, forceToOriginal: true);

        if (rb != null)
        {
            rbHadKinematic = rb.isKinematic;
            rbHadUseGravity = rb.useGravity;
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        isFollowing = true;
        CurrentRightHandHeld = this;
    }

    public void PlaceOn(Transform placementPoint)
    {
        if (!isFollowing || placementPoint == null) return;

        // Seguridad: si está cerca del punto, encaja exacto
        float dist = Vector3.Distance(_tr.position, placementPoint.position);
        if (dist <= snapDistance)
        {
            _tr.position = placementPoint.position;
            _tr.rotation = placementPoint.rotation;
        }
        else
        {
            // Si no está cerca, lo forzamos igualmente (puedes exigir cercanía si prefieres)
            _tr.position = placementPoint.position;
            _tr.rotation = placementPoint.rotation;
        }

        StopFollowingHand(true);
    }

    private void StopFollowingHand(bool restorePhysics)
    {
        isFollowing = false;
        if (restorePhysics && rb != null)
        {
            rb.isKinematic = rbHadKinematic;
            rb.useGravity = rbHadUseGravity;
        }

        if (CurrentRightHandHeld == this)
            CurrentRightHandHeld = null;
    }

    // ---------- Highlight helpers ----------
    private void SetHighlight(bool on, bool forceToOriginal = false)
    {
        if (renderers == null || originalColors == null) return;

        if (forceToOriginal)
        {
            for (int i = 0; i < renderers.Length; i++) SetColor(renderers[i], originalColors[i]);
            return;
        }

        if (on)
        {
            for (int i = 0; i < renderers.Length; i++) SetColor(renderers[i], highlightColor);
        }
        else
        {
            for (int i = 0; i < renderers.Length; i++) SetColor(renderers[i], originalColors[i]);
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
}