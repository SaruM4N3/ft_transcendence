using Unity.Netcode;
using UnityEngine;

// Server-authoritative chase-and-melee AI: moves toward the nearest player and deals periodic
// contact damage on arrival. Movement replicates to clients via NetworkTransform (Server authority).
public class EnemyAI : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float attackCooldown = 1f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private float nextAttackTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        // Always chasing, so just play the run cycle once - no idle/attack blend tree to drive.
        if (animator != null)
            animator.Play("Run");
    }

    void FixedUpdate()
    {
        if (!IsServer)
            return;

        Transform target = FindNearestPlayer();
        if (target == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toTarget = (Vector2)target.position - rb.position;
        float distance = toTarget.magnitude;

        if (distance <= attackRange)
        {
            rb.linearVelocity = Vector2.zero;
            if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + attackCooldown;
                target.GetComponent<IDamageable>()?.RequestDamage(damage);
            }
            return;
        }

        Vector2 direction = toTarget.normalized;
        rb.linearVelocity = direction * moveSpeed;
        if (spriteRenderer != null && Mathf.Abs(direction.x) > 0.01f)
            spriteRenderer.flipX = direction.x < 0f;
    }

    private Transform FindNearestPlayer()
    {
        Transform nearest = null;
        float nearestDistSq = float.MaxValue;
        foreach (PlayerCustomization player in PlayerCustomization.AllActiveInstances)
        {
            float distSq = ((Vector2)player.transform.position - rb.position).sqrMagnitude;
            if (distSq < nearestDistSq)
            {
                nearestDistSq = distSq;
                nearest = player.transform;
            }
        }
        return nearest;
    }
}
