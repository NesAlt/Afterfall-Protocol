using UnityEngine;

public class SampleManager : MonoBehaviour
{
    public static SampleManager Instance;

    [Header("Objective")]
    public int currentSamples = 0;
    public int requiredSamples = 10;

    private void Awake()
    {
        Instance = this;
    }

    public void AddSample(int amount)
    {
        currentSamples += amount;

        Debug.Log($"Samples: {currentSamples}/{requiredSamples}");

        if (currentSamples >= requiredSamples)
        {
            CompleteLevel();
        }
    }

    void CompleteLevel()
    {
        Debug.Log("LEVEL COMPLETE");

        // TODO:
        // - Open exit door
        // - Trigger UI
        // - Notify game manager
    }
}