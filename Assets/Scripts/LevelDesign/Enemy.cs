using UnityEngine;

public class Enemy : MonoBehaviour
{
    private ArenaController arena;
    private EnemySpawner spawner;
    private bool isFinalEnemy;

    [Header("Drop Settings")]
    [SerializeField] private GameObject samplePrefab;
    [SerializeField] private float dropChance = 0.5f;

    public void Initialize(ArenaController arenaController, EnemySpawner enemySpawner, bool final, Transform playerTransform)
    {
        arena = arenaController;
        spawner = enemySpawner;
        isFinalEnemy = final;

        EnemyAI ai = GetComponent<EnemyAI>();
        if(ai != null)
        {
            ai.player = playerTransform;
        }
    }

    public void Die()
    {
        TryDropSample();

        arena.RegisterKill(isFinalEnemy);
        spawner.NotifyEnemyDeath();
        Destroy(gameObject);
    }
    void TryDropSample()
    {
        if (samplePrefab == null) return;

        if (LevelManager.Instance == null || !LevelManager.Instance.IsKillAndCollectLevel())
            return;

        if (Random.value <= dropChance)
        {
            Instantiate(samplePrefab, transform.position, Quaternion.identity);
        }
    }
}
