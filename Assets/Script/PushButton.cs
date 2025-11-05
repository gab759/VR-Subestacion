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

    [Header("Dependencies (palancas)")]
    [Tooltip("Al menos una de estas palancas debe tener isOpen = true")]
    public HingedDoor[] requiredLevers;

    [Header("Visual Feedback (opcional)")]
    public Renderer buttonRenderer;
    public Color highlightColor = Color.yellow;
    public Color blockedColor = Color.red; // color breve cuando está bloqueado

    [Header("Events")]
    public UnityEvent onPressed; // se dispara cuando el botón se presiona (condición cumplida)
    public UnityEvent onBlocked; // se dispara cuando NO se cumple la condición

    // Interno
    private Vector3 initialLocalPos;
    private bool isAnimating;
    private Color originalColor;

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

    public void Interact()
    {
        if (isAnimating) return;

        if (!IsAnyLeverOpen())
        {
            // Feedback bloqueado
            if (buttonRenderer != null)
                StartCoroutine(FlashColor(blockedColor, 0.15f));
            onBlocked?.Invoke();
            return;
        }

        // Condición OK → presionar
        StartCoroutine(PressRoutine());
    }

    private bool IsAnyLeverOpen()
    {
        if (requiredLevers == null || requiredLevers.Length == 0) return false;

        for (int i = 0; i < requiredLevers.Length; i++)
        {
            var lever = requiredLevers[i];
            if (lever != null && lever.isOpen) return true;
        }
        return false;
    }

    private System.Collections.IEnumerator PressRoutine()
    {
        isAnimating = true;

        Vector3 pressedPos = initialLocalPos + new Vector3(0f, 0f, -pressDistance);

        // Animar hacia adentro
        yield return MoveLocal(transform, initialLocalPos, pressedPos, moveTime);

        // Evento al presionar
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

        var mat = buttonRenderer.material;
        Color prev = mat.color;
        mat.color = c;
        yield return new WaitForSeconds(time);
        mat.color = prev;
    }

    public void OnSelect()
    {
        if (buttonRenderer != null)
            buttonRenderer.material.color = highlightColor;
    }

    public void OnDeselect()
    {
        if (buttonRenderer != null)
            buttonRenderer.material.color = originalColor;
    }
}