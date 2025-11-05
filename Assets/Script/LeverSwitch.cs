using UnityEngine;

public class HingedDoor : MonoBehaviour, IInteractable
{
    [Header("Door Settings")]
    [Tooltip("Estado inicial de la puerta")]
    public bool isOpen = false;

    [Tooltip("Ángulo de apertura (grados en eje X). Usa negativo para invertir sentido.")]
    public float openAngle = 90f;

    [Tooltip("Velocidad de animación en grados/segundo")]
    public float animationSpeed = 120f;

    [Header("Pivot (punto de rotación)")]
    [Tooltip("Transform del pivote/bisagra. Si está vacío, usa este transform.")]
    public Transform rotationPivot;

    [Header("Visual Feedback")]
    public Color highlightColor = Color.yellow;

    private Renderer doorRenderer;
    private Color originalColor;

    // Control interno
    private float currentAngleX = 0f; // 0 = cerrada
    private float targetAngleX = 0f;  // 0 o openAngle
    private bool isAnimating = false;

    void Start()
    {
        if (rotationPivot == null)
            rotationPivot = transform;

        doorRenderer = GetComponent<Renderer>();
        if (doorRenderer != null)
            originalColor = doorRenderer.material.color;

        targetAngleX = isOpen ? openAngle : 0f;

        // Si inicia abierta, aplica rotación inicial
        if (isOpen && Mathf.Abs(currentAngleX - targetAngleX) > 0.01f)
        {
            float delta = targetAngleX - currentAngleX;
            transform.RotateAround(rotationPivot.position, Vector3.right, delta); // X
            currentAngleX = targetAngleX;
        }
    }

    void Update()
    {
        if (!isAnimating) return;

        float nextAngle = Mathf.MoveTowards(currentAngleX, targetAngleX, animationSpeed * Time.deltaTime);
        float delta = nextAngle - currentAngleX;

        // Rotación en X alrededor del pivote (espacio mundo)
        transform.RotateAround(rotationPivot.position, Vector3.right, delta);

        currentAngleX = nextAngle;

        if (Mathf.Approximately(currentAngleX, targetAngleX))
            isAnimating = false;
    }

    public void Interact()
    {
        if (isAnimating) return;

        isOpen = !isOpen;
        targetAngleX = isOpen ? openAngle : 0f;
        isAnimating = true;
    }

    public void OnSelect()
    {
        if (doorRenderer != null)
            doorRenderer.material.color = highlightColor;
    }

    public void OnDeselect()
    {
        if (doorRenderer != null)
            doorRenderer.material.color = originalColor;
    }
}
