using UnityEngine;
using System;
using UnityEngine.Events;

public class SlidingDoor : MonoBehaviour, IInteractable
{
    [Header("Movimiento")]
    [Tooltip("Transform que se va a rotar. Si está vacío, se usa este mismo objeto.")]
    public Transform pivot;

    public bool isOpen = false;
    public float openAngle = 90f;
    public float animationSpeed = 2f;
    public Color highlightColor = Color.yellow;

    public static event Action<bool> OnInterruptorStateChanged;

    [Header("Eventos procedimiento")]
    public UnityEvent onOpen;   // se dispara cuando pasa a isOpen = true
    public UnityEvent onClose;  // se dispara cuando pasa a isOpen = false

    public bool IsOpen => isOpen;

    [Header("Checklist bloqueo / hint")]
    [SerializeField] private CheckList checklist;

    [Tooltip("Pasos en los que se permite usar ESTE objeto (por orden exacto de nombres en el CheckList).")]
    [SerializeField] private string[] allowedSteps;

    [Tooltip("Paso para el que este objeto dará la pista visual (parpadeo).")]
    [SerializeField] private string hintStepName;

    [SerializeField] private float hintDelay = 10f;
    [SerializeField] private float blinkSpeed = 4f;

    private bool lockUntilCorrectStep = true;

    private Quaternion closedRotation;
    private Quaternion openRotation;
    private Renderer[] renderers;
    private Color[] originalColors;

    // hint
    private float hintTimer = 0f;
    private bool isBlinking = false;

    void Start()
    {
        if (pivot == null)
            pivot = transform;

        closedRotation = pivot.localRotation;
        openRotation = closedRotation * Quaternion.Euler(openAngle, 0, 0);

        // Obtener TODOS los renderers del pivot y sus hijos
        renderers = pivot.GetComponentsInChildren<Renderer>();

        if (renderers != null && renderers.Length > 0)
        {
            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    originalColors[i] = renderers[i].material.color;
            }
        }
        else
        {
            Debug.LogWarning($"[SlidingDoor] {name} no encontró ningún Renderer para el highlight.");
        }

        Invoke(nameof(NotifyInitialState), 0.1f);
    }

    void NotifyInitialState()
    {
        OnInterruptorStateChanged?.Invoke(!isOpen);
    }

    void Update()
    {
        // animación
        Quaternion targetRotation = isOpen ? openRotation : closedRotation;
        pivot.localRotation = Quaternion.Slerp(
            pivot.localRotation,
            targetRotation,
            Time.deltaTime * animationSpeed
        );

        // sistema de hint
        UpdateHint();
    }

    public void Interact()
    {
        // 1) Bloqueo por checklist
        if (lockUntilCorrectStep && checklist != null && allowedSteps != null && allowedSteps.Length > 0)
        {
            string current = checklist.GetCurrentItemName();
            bool allowed = false;

            foreach (var step in allowedSteps)
            {
                if (!string.IsNullOrEmpty(step) && current == step)
                {
                    allowed = true;
                    break;
                }
            }

            // Si no es el paso que toca, NO hacemos nada
            if (!allowed)
                return;
        }

        // 2) Cambio de estado
        isOpen = !isOpen;
        OnInterruptorStateChanged?.Invoke(!isOpen);

        if (isOpen)
            onOpen?.Invoke();
        else
            onClose?.Invoke();
    }

    public void OnSelect()
    {
        if (renderers == null || originalColors == null) return;

        // si está parpadeando por hint, no tocamos colores aquí
        if (isBlinking) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].material.color = highlightColor;
        }
    }

    public void OnDeselect()
    {
        if (renderers == null || originalColors == null) return;
        if (isBlinking) return; // el hint controla el color

        int len = Mathf.Min(renderers.Length, originalColors.Length);
        for (int i = 0; i < len; i++)
        {
            if (renderers[i] != null)
                renderers[i].material.color = originalColors[i];
        }
    }

    // -------- HINT: parpadeo tras X segundos sin completar el paso --------
    private void UpdateHint()
    {
        if (checklist == null || string.IsNullOrEmpty(hintStepName) || renderers == null || originalColors == null)
            return;

        string current = checklist.GetCurrentItemName();
        bool isCurrentStep = (current == hintStepName);
        bool isCompleted = checklist.IsItemCompleted(hintStepName);

        if (isCurrentStep && !isCompleted)
        {
            hintTimer += Time.deltaTime;

            if (hintTimer >= hintDelay)
            {
                isBlinking = true;

                // parpadeo simple: alterna entre color original y highlight
                bool on = Mathf.FloorToInt(Time.time * blinkSpeed) % 2 == 0;

                int len = Mathf.Min(renderers.Length, originalColors.Length);
                for (int i = 0; i < len; i++)
                {
                    if (renderers[i] == null) continue;
                    renderers[i].material.color = on ? highlightColor : originalColors[i];
                }
            }
        }
        else
        {
            // no es el paso actual o ya se completó => resetear
            hintTimer = 0f;

            if (isBlinking)
            {
                isBlinking = false;

                int len = Mathf.Min(renderers.Length, originalColors.Length);
                for (int i = 0; i < len; i++)
                {
                    if (renderers[i] != null)
                        renderers[i].material.color = originalColors[i];
                }
            }
        }
    }
}
