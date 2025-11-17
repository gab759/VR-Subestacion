using System.Collections.Generic;
using UnityEngine;

public class GameScoreManager : MonoBehaviour
{
    public static GameScoreManager Instance { get; private set; }

    [Header("Puntaje global")]
    public int startScore = 100;
    public int minScore = 0;

    [System.Serializable]
    public class CategoryConfig
    {
        public ScoreCategory category;
        public int penaltyPerMistake = 5;
    }

    [Header("Penalizaciones por categoría")]
    public List<CategoryConfig> categoryConfigs = new List<CategoryConfig>();

    public int CurrentScore { get; private set; }

    // Para estadísticas
    private Dictionary<ScoreCategory, int> mistakesByCategory = new Dictionary<ScoreCategory, int>();
    private Dictionary<ScoreCategory, int> correctByCategory = new Dictionary<ScoreCategory, int>();
    private Dictionary<ScoreCategory, int> penaltyLookup = new Dictionary<ScoreCategory, int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildPenaltyLookup();
        ResetScore();
    }

    private void BuildPenaltyLookup()
    {
        penaltyLookup.Clear();
        foreach (var cfg in categoryConfigs)
        {
            penaltyLookup[cfg.category] = cfg.penaltyPerMistake;
        }
    }

    public void ResetScore()
    {
        CurrentScore = startScore;
        mistakesByCategory.Clear();
        correctByCategory.Clear();
    }

    public void RegisterCorrect(ScoreCategory category)
    {
        if (!correctByCategory.ContainsKey(category))
            correctByCategory[category] = 0;

        correctByCategory[category]++;
        Debug.Log($"✔ Correcto ({category}). Puntos: {CurrentScore}");
    }

    public void RegisterMistake(ScoreCategory category)
    {
        if (!mistakesByCategory.ContainsKey(category))
            mistakesByCategory[category] = 0;

        mistakesByCategory[category]++;

        int penalty = 0;
        if (!penaltyLookup.TryGetValue(category, out penalty))
        {
            // Si no está configurado, usamos 5 por defecto
            penalty = 5;
        }

        CurrentScore -= penalty;
        if (CurrentScore < minScore)
            CurrentScore = minScore;

        Debug.Log($"✖ Error ({category}). Puntos: {CurrentScore}");
    }

    public int GetMistakes(ScoreCategory category)
    {
        return mistakesByCategory.TryGetValue(category, out var count) ? count : 0;
    }

    public int GetCorrects(ScoreCategory category)
    {
        return correctByCategory.TryGetValue(category, out var count) ? count : 0;
    }
}
