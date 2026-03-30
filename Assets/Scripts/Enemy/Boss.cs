using UnityEngine;

public abstract class Boss : MonoBehaviour
{
    protected ArenaController arena;

    public virtual void Initialize(ArenaController arenaController)
    {
        arena = arenaController;
    }

    public abstract void OnBossDeath();
}