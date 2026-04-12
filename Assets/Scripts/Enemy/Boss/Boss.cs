using UnityEngine;

public abstract class Boss : MonoBehaviour
{
    protected ArenaController arena;
    protected Transform playerTransform;

    public virtual void Initialize(ArenaController arenaController, Transform player)
    {
        arena           = arenaController;
        playerTransform = player;
    }

    public abstract void OnBossDeath();
}