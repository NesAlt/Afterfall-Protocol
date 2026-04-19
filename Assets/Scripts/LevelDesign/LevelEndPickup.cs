using UnityEngine;

public class LevelEndItem : MonoBehaviour
{
    public VictoryUIController victoryUI;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        LevelManager.Instance?.NotifyLevelCleared();

        victoryUI?.ShowVictory();

        Destroy(gameObject);
    }
}