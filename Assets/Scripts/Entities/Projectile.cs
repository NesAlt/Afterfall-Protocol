using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 12f;
    public float lifetime = 2f;

    private Vector2 direction;

    public void Init(Vector2 dir)
    {
        direction = dir.normalized;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        // Ignore player
        // if (col.CompareTag("Player"))
        //     return;

        // // Enemy hit
        // if (col.CompareTag("Enemy"))
        // {
        //     col.GetComponent<Health>()?.TakeDamage(1);
        //     Destroy(gameObject);
        //     return;
        // }

        // World / buildings
        if (col.gameObject.layer == LayerMask.NameToLayer("Ground") ||
            col.gameObject.layer == LayerMask.NameToLayer("Building"))
        {
            Destroy(gameObject);
        }
    }
}
