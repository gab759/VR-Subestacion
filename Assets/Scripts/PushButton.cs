using UnityEngine;
using UnityEngine.Events;

public class PushButton : MonoBehaviour, IInteractable
{
    [Header("Button Settings")]
    [Tooltip("Distancia que se moverá el botón en -Z local al presionar")]
    public float pressDistance = 0.03f;

    [Tooltip("Tiempo que permanece presionado antes de volver")]
    public float holdTime = 0.5f;

    [Tooltip("Tiempo de animación para ir/volver (suavizado)")]
    public float moveTime = 0.05f;

    [Header("Dependencies (opcionales)")]
    [Tooltip("Al menos una de estas SlidingDoor debe estar abierta (IsOpen = true). Si está vacío, no se usa dependencia.")]
    public SlidingDoor[] requiredDoors;

    [Header("Checklist bloqueo / hint")]
    [SerializeField] private CheckList checklist;

    [Tooltip("Pasos en los que se permite usar ESTE botón (deben coincidir con itemName del CheckList).")]
    [SerializeField] private string[] allowedSteps;

    [Tooltip("Paso para el que este botón dará la pista visual (parpadeo).")]
    [SerializeField] private string hintStepName;

    [SerializeField] private float hintDelay = 10f;
    [SerializeField] private float blinkSpeed = 4f;

    [Header("Visual Feedback")]
    public Renderer buttonRenderer;
    public Color highlightColor = Color.yellow;
    public Color blockedColor = Color.red; // color breve cuando está bloqueado por dependencia

    [Header("Events")]
    public UnityEvent onPressed; // se dispara cuando el botón se presiona (condición cumplida)
    public UnityEvent onBlocked; // se dispara cuando NO se cumple la condición (dependencia)

    // Interno
    private Vector3 initialLocalPos;
    private bool isAnimating;
    private Color originalColor;

    // Hint
    private float hintTimer = 0f;
    private bool isBlinking = false;

    void Reset()
    {
        if (buttonRenderer == null) buttonRenderer = GetComponent<Renderer>();
    }

    void Start()
    {
        initialLocalPos = transform.localPosition;

        if (buttonRenderer != null)
            originalColor = buttonRenderer.material.color;
    }

    void Update()
    {
        UpdateHint();
    }

    public void Interact()
    {
        if (isAnimating) return;

        // 1) bloqueo por checklist: solo si el paso actual está en allowedSteps
        if (checklist != null && allowedSteps != null && allowedSteps.Length > 0)
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

            if (!allowed)
            {
                // paso equivocado → no hace nada
                return;
            }
        }

        // 2) condición de SlidingDoor (si las usas)
        if (!IsAnyDoorOpen())
        {
            // Feedback bloqueado
            if (buttonRenderer != null)
                StartCoroutine(FlashColor(blockedColor, 0.15f));

            onBlocked?.Invoke();
            return;
        }

        // 3) condición OK → presionar
        StartCoroutine(PressRoutine());
    }

    private bool IsAnyDoorOpen()
    {
        // Si no configuraste ninguna puerta, no bloqueamos por dependencia
        if (requiredDoors == null || requiredDoors.Length == 0)
            return true;

        for (int i = 0; i < requiredDoors.Length; i++)
        {
            var door = requiredDoors[i];
            if (door != null && door.IsOpen) // usamos la propiedad de SlidingDoor
                return true;
        }
        return false;
    }

    private System.Collections.IEnumerator PressRoutine()
    {
        isAnimating = true;

        Vector3 pressedPos = initialLocalPos + new Vector3(0f, 0f, -pressDistance);

        // Animar hacia adentro
        yield return MoveLocal(transform, initialLocalPos, pressedPos, moveTime);

        // Evento al presionar (aquí normalmente enganchas el SubstationProcedureManager)
        onPressed?.Invoke();

        // Mantener
        yield return new WaitForSeconds(holdTime);

        // Volver a la posición original
        yield return MoveLocal(transform, pressedPos, initialLocalPos, moveTime);

        isAnimating = false;
    }

    private System.Collections.IEnumerator MoveLocal(Transform tr, Vector3 from, Vector3 to, float duration)
    {
        if (duration <= 0f)
        {
            tr.localPosition = to;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            tr.localPosition = Vector3.Lerp(from, to, k);
            yield return null;
        }
        tr.localPosition = to;
    }

    private System.Collections.IEnumerator FlashColor(Color c, float time)
    {
        if (buttonRenderer == null) yield break;
        if (isBlinking) yield break; // si está en hint, no pisamos el parpadeo

        var mat = buttonRenderer.material;
        Color prev = mat.color;
        mat.color = c;
        yield return new WaitForSeconds(time);
        mat.color = prev;
    }

    public void OnSelect()
    {
        if (buttonRenderer == null) return;
        if (isBlinking) return; // el hint controla el color

        buttonRenderer.material.color = highlightColor;
    }

    public void OnDeselect()
    {
        if (buttonRenderer == null) return;
        if (isBlinking) return;

        buttonRenderer.material.color = originalColor;
    }

    // ---------- HINT: parpadeo después de X segundos sin completar el paso ----------
    private void UpdateHint()
    {
        if (checklist == null || string.IsNullOrEmpty(hintStepName) || buttonRenderer == null)
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

                bool on = Mathf.FloorToInt(Time.time * blinkSpeed) % 2 == 0;
                buttonRenderer.material.color = on ? highlightColor : originalColor;
            }
        }
        else
        {
            // no es el paso actual o ya se completó => resetear
            hintTimer = 0f;

            if (isBlinking)
            {
                isBlinking = false;
                buttonRenderer.material.color = originalColor;
            }
        }
    }
}
