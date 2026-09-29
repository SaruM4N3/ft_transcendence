using UnityEngine;

// Local one-shot ranged FX, travels independently; kinematic Rigidbody2D avoids tunneling through small colliders at speed.
[RequireComponent(typeof(Rigidbody2D))]
public class ArrowProjectile : MonoBehaviour, IAttackFX
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float maxLifetime = 2f;

    private GameObject owner;
    private bool hasHitbox;
    private float damage;

    public void Init(GameObject attacker, bool hasHitboxValue, float damageValue)
    {
        owner = attacker;
        hasHitbox = hasHitboxValue;
        damage = damageValue;
    }

    void Awake()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.linearVelocity = transform.right * speed;
        Destroy(gameObject, maxLifetime);
    }

    // Stops on the first solid thing it hits (walls included), even though only the attacker's copy deals damage. Trigger-only zones (interaction, etc.) are ignored.
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.isTrigger)
            return;

        var damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            var behaviour = damageable as MonoBehaviour;
            if (behaviour != null && behaviour.gameObject == owner)
                return;

            if (hasHitbox)
                damageable.RequestDamage(damage);
        }

        Destroy(gameObject);
    }
}
