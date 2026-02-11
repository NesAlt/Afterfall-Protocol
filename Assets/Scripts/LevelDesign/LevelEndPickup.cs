using UnityEngine;

public class LevelEndItem : MonoBehaviour
{
    public VictoryUIController victoryUI;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Debug.Log("Pickup triggered");
            victoryUI.ShowVictory();
            Destroy(gameObject);
        }
    }
}
