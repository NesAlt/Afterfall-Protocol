using UnityEngine;

public abstract class Boss : MonoBehaviour
{
    private float stateTimer;
    protected ArenaController arena;

    public virtual void Initialize(ArenaController arenaController)
    {
        arena = arenaController;
    }

    public abstract void OnBossDeath();
}