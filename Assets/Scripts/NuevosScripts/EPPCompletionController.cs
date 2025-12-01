using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class EPPCompletionController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CheckList checklist;          // EPP_ChecklistManager

    // Panel / contenedor donde están los ítems de la lista
    [SerializeField] private GameObject checklistItemsRoot;

    // Texto de puntaje final: "Puntaje: XX"
    [SerializeField] private TMP_Text scoreText;

    // NUEVO: texto para el temporizador: "Cambiando en X..."
    [SerializeField] private TMP_Text countdownText;

    [Header("Escena siguiente")]
    [SerializeField] private string nextSceneName = "EscenaMantenimiento";
    [SerializeField] private float autoLoadDelay = 5f;

    private bool handled = false;

    private void Start()
    {
        if (scoreText != null)
            scoreText.gameObject.SetActive(false);

        if (countdownText != null)
            countdownText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (handled || checklist == null)
            return;

        if (checklist.AreAllItemsCompleted())
        {
            handled = true;
            OnEPPCompleted();
        }
    }

    private void OnEPPCompleted()
    {
        // 1) Ocultar la lista
        if (checklistItemsRoot != null)
            checklistItemsRoot.SetActive(false);

        // 2) Puntaje final
        int finalScore = 0;
        if (GameScoreManager.Instance != null)
            finalScore = GameScoreManager.Instance.CurrentScore;

        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(true);
            scoreText.text = $"Puntaje: {finalScore}";
        }

        // 3) Activar texto del temporizador
        if (countdownText != null)
            countdownText.gameObject.SetActive(true);

        Debug.Log($"EPP completados. Puntaje final: {finalScore}");

        // 4) Empezar cuenta regresiva y cambiar de escena
        if (!string.IsNullOrEmpty(nextSceneName))
            StartCoroutine(LoadNextSceneAfterDelay());
    }

    private IEnumerator LoadNextSceneAfterDelay()
    {
        float timeLeft = autoLoadDelay;

        while (timeLeft > 0f)
        {
            if (countdownText != null)
            {
                int seconds = Mathf.CeilToInt(timeLeft);
                countdownText.text = $"Cambiando en {seconds}...";
            }

            timeLeft -= Time.deltaTime;
            yield return null;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    // Si luego quieres usar un botón en vez del auto-load:
    public void GoToNextScene()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }
}
