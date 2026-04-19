using UnityEngine;

public class PlayerSpriteEvents : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;

    public void FireProjectile()
    {
        if (playerController != null)
            playerController.FireProjectile();
    }

    public void OnAttackStart()
    {
        if (playerController != null)
            playerController.isAttacking = true;
    }

    public void OnAttackEnd()
    {
        if (playerController != null)
            playerController.isAttacking = false;
    }
}