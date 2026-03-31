using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    private ArenaController arena;
    private EnemySpawner spawner;
    private bool isFinalEnemy;
    private SpawnPoint spawnPoint;
    public static List<Enemy> ActiveEnemies = new List<Enemy>();

    [Header("Drop Settings")]
    [SerializeField] private GameObject samplePrefab;
    [SerializeField] private float dropChance = 0.5f;
    private float dropChanceMultiplier = 1f;


    void OnEnable()
    {
        ActiveEnemies.Add(this);
    }

    void OnDisable()
    {
        ActiveEnemies.Remove(this);
    }
    public void Initialize(ArenaController arenaController, EnemySpawner enemySpawner, bool final, 
                        Transform playerTransform, SpawnPoint spawnOrigin, float dropMultiplier = 1f)
    {
        arena = arenaController;
        spawner = enemySpawner;
        isFinalEnemy = final;
        spawnPoint = spawnOrigin;
        dropChanceMultiplier = dropMultiplier;

        EnemyAI ai = GetComponent<EnemyAI>();
        if (ai != null)
            ai.player = playerTransform;
    }


    public void Die()
    {
        TryDropSample();

        arena.RegisterKill(isFinalEnemy);
        spawner.NotifyEnemyDeath(spawnPoint);
        Destroy(gameObject);
    }
    void TryDropSample()
    {
        if (samplePrefab == null) return;

        if (LevelManager.Instance == null || !LevelManager.Instance.IsKillAndCollectLevel())
            return;

        if (Random.value <= dropChance * dropChanceMultiplier)
        {
        Vector3 offset = new Vector3(
            Random.Range(-0.3f, 0.3f),
            Random.Range(0.8f, 1.2f),
            0f
        );

        Instantiate(samplePrefab, transform.position + offset, Quaternion.identity);        
        }
    }
}
