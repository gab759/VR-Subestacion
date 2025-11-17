using System.Collections.Generic;
using UnityEngine;

public class PadlockGrabber : MonoBehaviour, IInteractable   // ← IMPORTANTE
{
    [Header("Refs")]
    [Tooltip("Transform del controlador/mano derecha (puedes usar tu rayOrigin)")]
    public Transform rightHandAnchor;

    [Tooltip("Punto donde se coloca el candado (pose final)")]
    public Transform placementPoint;

    [Tooltip("Capa del collider (trigger) del placementPoint para el raycast")]
    public LayerMask placementLayer = ~0;

    [Header("Follow (no parent)")]
    [Tooltip("Offset local de posición respecto a la mano")]
    public Vector3 localPositionOffset = Vector3.zero;

    [Tooltip("Offset local de rotación (Euler) respecto a la mano")]
    public Vector3 localEulerOffset = Vector3.zero;

    [Tooltip("Suavizado de seguimiento (0 = sin suavizado)")]
    [Range(0f, 40f)]
    public float followSmoothing = 20f;

    [Header("Colocación")]
    [Tooltip("Distancia máxima para considerar 'cerca' del punto y permitir encajar")]
    public float snapDistance = 0.12f;

    [Header("Highlight")]
    public Color highlightColor = Color.yellow;

    // Internos
    private Renderer[] renderers;
    private Color[] originalColors;
    private bool isFollowing = false;   // en vez de isGrabbed/parent
    private bool isHighlighted = false;
    private Transform _tr;

    void Awake()
    {
        _tr = transform;

        // Cache renderers + colores (soporta URP/Standard)
        renderers = GetComponentsInChildren<Renderer>(true);
        var cols = new List<Color>(renderers.Length);
        foreach (var r in renderers) cols.Add(GetColor(r));
        originalColors = cols.ToArray();
    }

    void LateUpdate()
    {
        if (!isFollowing || rightHandAnchor == null) return;

        // Objetivo de seguimiento (sin parent)
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

    // ===== IInteractable =====
    public void OnSelect()
    {
        if (isFollowing) return; // ya en mano → no resaltar
        SetHighlight(true);
    }

    public void OnDeselect()
    {
        if (isFollowing) return;
        SetHighlight(false);
    }

    public void Interact()
    {
        if (!isFollowing)
        {
            StartFollowingHand();   // empezar a seguir mano
            return;
        }

        // Si ya sigue, intenta colocar si apunto al placement o estoy cerca
        if (CanPlaceNow())
            PlaceOnPoint();
    }
    // =========================

    private void StartFollowingHand()
    {
        if (rightHandAnchor == null)
        {
            Debug.LogWarning("[Padlock] Falta rightHandAnchor.");
            return;
        }

        // quitar highlight (volver a original)
        SetHighlight(false, forceToOriginal: true);

        // activar modo seguimiento (no parent)
        isFollowing = true;
    }

    private bool CanPlaceNow()
    {
        if (placementPoint == null) return false;

        // 1) Estoy apuntando al placement con el rayo de la mano
        bool aiming = IsAimingPlacementByRay();

        // 2) O estoy suficientemente cerca (seguridad por si falla el rayo)
        float dist = Vector3.Distance(_tr.position, placementPoint.position);
        bool near = dist <= snapDistance;

        return aiming || near;
    }

    private void PlaceOnPoint()
    {
        if (placementPoint == null)
        {
            Debug.LogWarning("[Padlock] Falta placementPoint.");
            return;
        }

        // Fijar pose exacta y salir del modo seguimiento
        _tr.position = placementPoint.position;
        _tr.rotation = placementPoint.rotation;

        isFollowing = false;
    }

    private bool IsAimingPlacementByRay()
    {
        if (placementPoint == null || rightHandAnchor == null) return false;

        Vector3 origin = rightHandAnchor.position;
        Vector3 dir = rightHandAnchor.forward;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, 5f, placementLayer, QueryTriggerInteraction.Collide))
        {
            return hit.transform == placementPoint || hit.transform.IsChildOf(placementPoint);
        }
        return false;
    }

    // ---------- Highlight helpers (URP/Standard) ----------
    private void SetHighlight(bool on, bool forceToOriginal = false)
    {
        if (forceToOriginal)
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
}