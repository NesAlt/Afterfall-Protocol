using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Component on gameobjects with colliders which determines if there is
/// a collider overlapping them which is on a specific layer.
/// Used to check for ground.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class GroundCheck : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("The layers which are considered \"Ground\".")]
    public LayerMask groundLayers = new LayerMask();
    [Tooltip("The collider to check with. (Defaults to the collider on this game object.)")]
    public Collider2D groundCheckCollider = null;

    [Header("Effect Settings")]
    [Tooltip("The effect to create when landing")]
    public GameObject landingEffect;

    // Whether or not the player was grounded last check
    [HideInInspector]
    public bool groundedLastCheck = false;

    /// <summary>
    /// Description:
    /// Standard Unity function called once before the first update
    /// Input: 
    /// none
    /// Return: 
    /// void (no return)
    /// </summary>
    private void Start()
    {
        // When this component starts up, ensure that the collider is not null, if possible
        GetCollider();
    }

    /// <summary>
    /// Description:
    /// Attempts to setup the collider to be used with ground checking,
    /// if one is not already set up.
    /// Input: 
    /// none
    /// Return: 
    /// void (no return)
    /// </summary>
    public void GetCollider()
    {
        if (groundCheckCollider == null)
        {
            groundCheckCollider = gameObject.GetComponent<Collider2D>();
        }
    }

    /// <summary>
    /// Description:
    /// Checks whether there is a collider overlapping the checking collider which is on a "ground" layer.
    /// Input: 
    /// none
    /// Return: 
    /// bool
    /// </summary>
    /// <returns>bool: Whether or not this collider counts as grounded</returns>
    public bool CheckGrounded()
    {
        if (groundCheckCollider == null)
            GetCollider();

        // Fire from slightly ABOVE the collider bottom so the ray isn't already
        // inside the platform when the rigidbody sinks on a fast landing
        const float skinWidth = 0.05f;
        Vector2 origin = new Vector2(
            groundCheckCollider.bounds.center.x,
            groundCheckCollider.bounds.min.y + skinWidth
        );

        float distance = 0.2f + skinWidth;

        RaycastHit2D hit = Physics2D.Raycast(
            origin,
            Vector2.down,
            distance,
            groundLayers
        );

        Debug.DrawRay(origin, Vector2.down * distance, hit ? Color.green : Color.red);

        if (hit.collider != null)
        {
            if (!groundedLastCheck && landingEffect)
            {
                Instantiate(landingEffect, transform.position, Quaternion.identity);
            }

            groundedLastCheck = true;
            return true;
        }

        groundedLastCheck = false;
        return false;
    }
}