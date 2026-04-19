using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    private void Start()
    {
        // When this component starts up, ensure that the collider is not null, if possible
        GetCollider();
    }

    public void GetCollider()
    {
        if (groundCheckCollider == null)
        {
            groundCheckCollider = gameObject.GetComponent<Collider2D>();
        }
    }

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