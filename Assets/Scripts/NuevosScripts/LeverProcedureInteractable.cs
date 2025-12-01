using UnityEngine;
using UnityEngine.Events;

public class LeverProcedureInteractable : MonoBehaviour, IInteractable
{
    [Header("Estado")]
    public bool isOn = false;          // false = posición inicial (arriba), true = activado (abajo)
    public float onAngle = 90f;        // cuánto gira en X al activarse
    public float animationSpeed = 2f;

    [Header("Visual")]
    public Color highlightColor = Color.yellow;

    [Header("Eventos")]
    public UnityEvent onTurnOn;        // se dispara al pasar a ON
    public UnityEvent onTurnOff;       // se dispara al pasar a OFF

    public bool IsOn => isOn;

    private Quaternion offRotation;
    private Quaternion onRotation;
    private Renderer[] renderers;
    private Color[] originalColors;

    void Start()
    {
        offRotation = transform.rotation;
        onRotation = offRotation * Quaternion.Euler(onAngle, 0f, 0f);

        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].material.color;
        }
    }

    void Update()
    {
        Quaternion target = isOn ? onRotation : offRotation;
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            target,
            Time.deltaTime * animationSpeed
        );
    }

    public void Interact()
    {
        isOn = !isOn;

        // quitar highlight al pulsar B
        ResetHighlight();

        if (isOn)
            onTurnOn?.Invoke();
        else
            onTurnOff?.Invoke();

        Debug.Log($"[LeverProcedureInteractable] {name} Interact. isOn = {isOn}");
    }

    public void OnSelect()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material.color = highlightColor;
        }
    }

    public void OnDeselect()
    {
        ResetHighlight();
    }

    private void ResetHighlight()
    {
        if (renderers == null) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material.color = originalColors[i];
        }
    }
}
