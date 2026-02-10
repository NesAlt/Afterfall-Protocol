using UnityEngine;
public class EnemyAI : MonoBehaviour
{
    public Transform player;
    public float chaseRange = 6f;
    public float attackRange = 1.2f;
    public float attackCooldown = 1.2f;

    private EnemyMotor motor;
    private Animator animator;
    private float lastAttack;

    void Awake()
    {
        motor = GetComponent<EnemyMotor>();
        animator = GetComponent<Animator>();
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
            motor.Move(dir);
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
