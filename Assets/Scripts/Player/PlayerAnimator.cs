using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("The player controller script to read state information from")]
    public PlayerController playerController;
    [Tooltip("The animator component that controls the player's animations")]
    public Animator animator;

    void Start()
    {
        ReadPlayerStateAndAnimate();
    }

    void Update()
    {
        ReadPlayerStateAndAnimate();
    }

   void ReadPlayerStateAndAnimate()
    {
        if (animator == null) return;

        animator.SetBool("isIdle",        playerController.state == PlayerController.PlayerState.Idle);
        animator.SetBool("isJumping",     playerController.state == PlayerController.PlayerState.Jump);
        animator.SetBool("isFalling",     playerController.state == PlayerController.PlayerState.Fall);
        animator.SetBool("isRunning",     playerController.state == PlayerController.PlayerState.Walk);
        animator.SetBool("isWallSliding", playerController.state == PlayerController.PlayerState.WallSlide);
        animator.SetBool("isWallJumping", playerController.state == PlayerController.PlayerState.WallJump);
        animator.SetBool("isDead",        playerController.state == PlayerController.PlayerState.Dead);
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
    
    public void PlayAttack()
    {
        if (animator == null) return;
        animator.SetTrigger("attack");
    }
    public void FireProjectile()
    {
        if (playerController != null)
        {
            playerController.FireProjectile();
        }
    }
}
