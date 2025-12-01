using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ProcedureToggleInteractable : MonoBehaviour, IInteractable
{
    [Header("Rotación")]
    [SerializeField] private Transform pivot;          // Si lo dejas vacío, usa este mismo objeto
    [SerializeField] private Vector3 localOffEuler;    // Rotación cuando está "arriba"/apagado
    [SerializeField] private Vector3 localOnEuler;     // Rotación cuando está "abajo"/activado
    [SerializeField] private float rotateSpeed = 180f; // grados por segundo

    [Header("Visual")]
    [SerializeField] private Color highlightColor = Color.yellow;

    [Header("Eventos")]
    public UnityEvent onTurnOn;   // Se dispara cuando pasa a estado ON
    public UnityEvent onTurnOff;  // Se dispara cuando pasa a estado OFF

    // Propiedad pública para que otros scripts sepan si está ON
    public bool IsOn => isOn;

    private bool isOn = false;
    private bool isMoving = false;
    private Quaternion targetRotation;

    private Renderer[] renderers;
    private Color[] originalColors;

    private void Awake()
    {
        if (pivot == null)
            pivot = transform;

        // Si no configuras localOffEuler en el inspector, usamos la rotación inicial
        if (localOffEuler == Vector3.zero)
            localOffEuler = pivot.localRotation.eulerAngles;

        // Guardar renderers para highlight (del pivot y sus hijos)
        renderers = pivot.GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].material.color;
        }
    }

    public void Interact()
    {
        if (isMoving) return;

        // Cambiamos el estado
        isOn = !isOn;
        Vector3 targetEuler = isOn ? localOnEuler : localOffEuler;
        targetRotation = Quaternion.Euler(targetEuler);

        // Quitamos el highlight al pulsar B
        ResetHighlight();

        StartCoroutine(RotateRoutine());

        if (isOn)
            onTurnOn?.Invoke();
        else
            onTurnOff?.Invoke();

        Debug.Log($"[ProcedureToggleInteractable] {name} Interact: isOn = {isOn}");
    }

    private IEnumerator RotateRoutine()
    {
        isMoving = true;

        while (Quaternion.Angle(pivot.localRotation, targetRotation) > 0.1f)
        {
            pivot.localRotation = Quaternion.RotateTowards(
                pivot.localRotation,
                targetRotation,
                rotateSpeed * Time.deltaTime
            );
            yield return null;
        }

        pivot.localRotation = targetRotation;
        isMoving = false;
    }

    public void OnSelect()
    {
        if (isMoving) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material.color = highlightColor;
        }
    }

    public void OnDeselect()
    {
        if (isMoving) return;
        ResetHighlight();
    }

    private void ResetHighlight()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material.color = originalColors[i];
        }
    }
}
