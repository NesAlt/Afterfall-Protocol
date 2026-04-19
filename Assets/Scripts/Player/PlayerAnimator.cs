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
        if (animator == null)
        {
            return;
        }
        if (playerController.state == PlayerController.PlayerState.Idle)
        {
            animator.SetBool("isIdle", true);
        }
        else
        {
            animator.SetBool("isIdle", false);
        }

        if (playerController.state == PlayerController.PlayerState.Jump)
        {
            animator.SetBool("isJumping", true);
        }
        else
        {
            animator.SetBool("isJumping", false);
        }

        if (playerController.state == PlayerController.PlayerState.Fall)
        {
            animator.SetBool("isFalling", true);
        }
        else
        {
            animator.SetBool("isFalling", false);
        }

        if (playerController.state == PlayerController.PlayerState.Walk)
        {
            animator.SetBool("isRunning", true);
        }
        else
        {
            animator.SetBool("isRunning", false);
        }

        if (playerController.state == PlayerController.PlayerState.Dead)
        {
            animator.SetBool("isDead", true);
        }
        else
        {
            animator.SetBool("isDead", false);
        }
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
