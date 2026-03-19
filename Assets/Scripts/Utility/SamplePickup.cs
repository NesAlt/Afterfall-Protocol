using UnityEngine;

public class SamplePickup : MonoBehaviour
{
    [SerializeField] private int value = 1;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        SampleManager.Instance.AddSample(value);

        Destroy(gameObject);
    }
}