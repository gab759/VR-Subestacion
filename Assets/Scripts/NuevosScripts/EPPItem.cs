using UnityEngine;

public class EPPItem : MonoBehaviour, IInteractable
{
    [Header("Checklist")]
    [SerializeField] private string itemName;   // Debe coincidir con el nombre en CheckList
    [SerializeField] private CheckList checklist;

    [Header("Piezas del EPP")]
    [Tooltip("Partes que se moverán al cuerpo (ej: bota derecha, bota izquierda). " +
             "Si lo dejas vacío se usa este mismo objeto.")]
    [SerializeField] private Transform[] worldParts;

    [Tooltip("Puntos del cuerpo donde se colocará cada parte. " +
             "Debe tener el mismo tamaño que worldParts.")]
    [SerializeField] private Transform[] attachPoints;

    [Header("Rotación al colocarse")]
    [Tooltip("Rotación local extra al colocarse (en grados). " +
             "Se aplica encima de la rotación del punto de anclaje.")]
    [SerializeField] private Vector3 localRotationOffsetEuler = Vector3.zero;

    [Header("Visual")]
    [SerializeField] private Color highlightColor = Color.yellow;

    private Renderer[] renderers;
    private Color[] originalColors;
    private bool isEquipped = false;

    private void Awake()
    {
        // Si no configuraste worldParts, usamos este mismo objeto como 1 parte
        if (worldParts == null || worldParts.Length == 0)
        {
            worldParts = new Transform[1] { transform };
        }

        // Guardar renderers y colores originales (para highlight)
        renderers = new Renderer[worldParts.Length];
        originalColors = new Color[worldParts.Length];

        for (int i = 0; i < worldParts.Length; i++)
        {
            renderers[i] = worldParts[i].GetComponent<Renderer>();
            if (renderers[i] != null)
                originalColors[i] = renderers[i].material.color;
        }
    }

    public void Interact()
    {
        if (isEquipped || checklist == null)
            return;

        string current = checklist.GetCurrentItemName();

        // Siempre que se presiona B, quitamos el highlight
        ResetHighlight();

        // ¿Es el EPP correcto para este paso del checklist?
        if (current == itemName)
        {
            // ✅ Correcto: marcar checklist y colocar en el cuerpo
            checklist.CompleteItem(itemName);
            EquipOnBody();
            isEquipped = true;

            // Avisar al score global (categoría EPP) – opcional
            if (GameScoreManager.Instance != null)
                GameScoreManager.Instance.RegisterCorrect(ScoreCategory.EPP);
        }
        else
        {
            // ❌ Incorrecto: solo restamos puntos, NO avanzamos checklist
            if (GameScoreManager.Instance != null)
                GameScoreManager.Instance.RegisterMistake(ScoreCategory.EPP);

            Debug.Log($"Te tocaba: {current}, no {itemName}");
        }
    }

    private void EquipOnBody()
    {
        if (attachPoints == null || attachPoints.Length == 0)
        {
            Debug.LogWarning($"[EPPItem] {name} no tiene attachPoints asignados.");
            return;
        }

        if (attachPoints.Length != worldParts.Length)
        {
            Debug.LogWarning($"[EPPItem] {name}: worldParts y attachPoints deben tener el mismo tamaño.");
            return;
        }

        Quaternion offsetRot = Quaternion.Euler(localRotationOffsetEuler);

        for (int i = 0; i < worldParts.Length; i++)
        {
            Transform part = worldParts[i];
            Transform point = attachPoints[i];

            if (part == null || point == null)
                continue;

            // Primero los hacemos hijos del punto
            part.SetParent(point);

            // Posición EXACTA del punto
            part.position = point.position;

            // Rotación = rotación del punto + offset extra
            part.rotation = point.rotation * offsetRot;

            // Opcional: desactivar collider para no seguir interactuando
            var col = part.GetComponent<Collider>();
            if (col != null)
                col.enabled = false;
        }
    }

    public void OnSelect()
    {
        if (isEquipped) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].material.color = highlightColor;
        }
    }

    public void OnDeselect()
    {
        if (isEquipped) return;
        ResetHighlight();
    }

    private void ResetHighlight()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].material.color = originalColors[i];
        }
    }
}
