using UnityEngine;

public class Enemy : MonoBehaviour
{
    private ArenaController arena;
    private EnemySpawner spawner;
    private bool isFinalEnemy;

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
        arena.RegisterKill(isFinalEnemy);
        spawner.NotifyEnemyDeath();
        Destroy(gameObject);
    }
}
