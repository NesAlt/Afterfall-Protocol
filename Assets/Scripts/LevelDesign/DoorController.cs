using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class DoorController : MonoBehaviour
{
    private Collider2D doorCollider;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        doorCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        OpenDoor();
    }

    public void CloseDoor()
    {
        doorCollider.enabled = true;
        spriteRenderer.enabled = true;
    }

    public void OpenDoor()
    {
        doorCollider.enabled = false;
        spriteRenderer.enabled = false;
    }
}
