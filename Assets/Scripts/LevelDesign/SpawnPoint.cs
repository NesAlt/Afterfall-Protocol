using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [HideInInspector] public int aliveCount = 0;

    [Header("Sample Drop Bonus")]
    [Tooltip("Multiplies the enemy drop chance at this point. Set higher for upper floors.")]
    public float dropChanceMultiplier = 1f;
}