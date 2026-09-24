using UnityEngine;

// Local one-shot ranged FX; travels independently (never parented to the attacker) and stops at
// the first thing it hits. Only the attacker's copy keeps a live hitbox.
// Moved via a kinematic Rigidbody2D (continuous collision) rather than raw Transform translation -
// at this speed, a plain transform.position += per-Update step is fast enough to tunnel clean
// through a small collider between physics steps.
[RequireComponent(typeof(Rigidbody2D))]
public class ArrowProjectile : MonoBehaviour, IAttackFX
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float damage = 12f;
    [SerializeField] private float maxLifetime = 2f;

    private Collider2D hitbox;
    private GameObject owner;
    private bool hasHitbox;

    public void Init(GameObject attacker, bool hasHitboxValue)
    {
        owner = attacker;
        hasHitbox = hasHitboxValue;
        if (!hasHitbox && hitbox != null)
            hitbox.enabled = false;
    }

    void Awake()
    {
        hitbox = GetComponent<Collider2D>();
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.linearVelocity = transform.right * speed;
        Destroy(gameObject, maxLifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!hasHitbox)
            return;

        var damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null)
            return;

        var behaviour = damageable as MonoBehaviour;
        if (behaviour != null && behaviour.gameObject == owner)
            return;

        damageable.RequestDamage(damage);
        Destroy(gameObject);
    }
}
