using UnityEngine;

public class PlayerSpriteEvents : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;

    public void FireProjectile()
    {
        if (playerController != null)
        {
            playerController.FireProjectile();
        }
    }
}
