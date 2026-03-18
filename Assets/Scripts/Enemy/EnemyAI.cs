using UnityEngine;
public class EnemyAI : MonoBehaviour
{
    public Transform player;
    public float chaseRange = 6f;
    public float attackRange = 1.2f;
    public float attackCooldown = 1.2f;
    public Animator animator;

    private EnemyMotor motor;
    private float lastAttack;

    [Header("Separation")]
    public float separationRadius = 1f;
    public float separationStrength = 2f;
    public LayerMask enemyLayer;

    void Awake()
    {
        motor = GetComponent<EnemyMotor>();
    }

    void Update()
    {
        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= attackRange)
        {
            motor.Stop();
            TryAttack();
        }
        else if (dist <= chaseRange)
        {
            float dir = Mathf.Sign(player.position.x - transform.position.x);

            Vector2 moveDir = new Vector2(dir, 0);

            // --- Separation ---
            Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, separationRadius, enemyLayer);

            Vector2 separation = Vector2.zero;

            foreach (var e in nearby)
            {
                if (e.gameObject != gameObject)
                {
                    Vector2 diff = (Vector2)(transform.position - e.transform.position);
                    separation += diff.normalized / diff.magnitude;
                }
            }

            moveDir += separation * separationStrength;

            motor.Move(moveDir.x);

            animator.SetFloat("Speed", 1);
        }
        else
        {
            motor.Stop();
            animator.SetFloat("Speed", 0);
        }
    }

    void TryAttack()
    {
        if (Time.time >= lastAttack + attackCooldown)
        {
            lastAttack = Time.time;
            animator.SetTrigger("Attack");
        }
    }

    public void OnDeath()
    {
        motor.Stop();
        enabled = false;
    }
}
