using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMotor : MonoBehaviour
{
    public float moveSpeed = 2f;

    private Rigidbody2D rb;
    private bool facingRight = true;
    private float moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Move(float direction)
    {
        moveInput = direction;
    }

    public void Stop()
    {
        moveInput = 0;
        rb.velocity = new Vector2(0, rb.velocity.y);
    }

    void FixedUpdate()
    {
        rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);
        HandleFlip(moveInput);
    }

    void HandleFlip(float x)
    {
        if (x > 0 && !facingRight || x < 0 && facingRight)
        {
            facingRight = !facingRight;
            Vector3 s = transform.localScale;
            s.x *= -1;
            transform.localScale = s;
        }
    }

    public void ApplyKnockback(Vector2 force)
    {
        rb.AddForce(force, ForceMode2D.Impulse);
    }
}
