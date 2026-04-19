// SampleManager.cs
using UnityEngine;

public class SampleManager : MonoBehaviour
{
    public static SampleManager Instance;

    public int currentSamples  = 0;
    public int requiredSamples = 10;

    [Header("UI")]
    [SerializeField] private SampleUI sampleUI;

    private bool levelCompleted = false;

    private void Awake() => Instance = this;

    private void Start() => UpdateUI();

    public void AddSample(int amount)
    {
        currentSamples += amount;
        UpdateUI();
        if (currentSamples >= requiredSamples)
            CompleteLevel();
    }

    void UpdateUI() => sampleUI?.UpdateUI(currentSamples, requiredSamples);

    void CompleteLevel()
    {
        if (levelCompleted) return;
        levelCompleted = true;

        Debug.Log("[SampleManager] Sample quota reached — level complete.");

        // 1. Notify run system — saves state, updates corruption, re-corruption rolls
        LevelManager.Instance?.NotifyLevelCleared();

        // 2. Stop spawning / open doors
        ArenaController arena = FindObjectOfType<ArenaController>();
        if (arena != null) arena.ForceEndArena();

        // 3. Show victory panel — VictoryUIController handles timeScale and navigation
        if (VictoryUIController.Instance != null)
            VictoryUIController.Instance.ShowVictory();
    }
}