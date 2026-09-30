using System.Collections.Generic;
using UnityEngine;

// Local one-shot ranged FX, travels independently; kinematic Rigidbody2D avoids tunneling through small colliders at speed.
[RequireComponent(typeof(Rigidbody2D))]
public class ArrowProjectile : MonoBehaviour, IAttackFX
{
    private GameObject owner;
    private bool hasHitbox;
    private float damage;
    private float speed;
    private float maxDistance;
    private int pierceCount;
    private int enemiesHit;
    private Vector3 startPosition;
    private Rigidbody2D rb;
    private readonly HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

    public void Init(GameObject attacker, bool hasHitboxValue, ClassKit kit)
    {
        owner = attacker;
        hasHitbox = hasHitboxValue;
        damage = kit.Damage;
        speed = kit.ProjectileSpeed;
        maxDistance = kit.ProjectileMaxDistance;
        pierceCount = kit.PierceCount;
        rb.linearVelocity = transform.right * speed;
    }

    void Awake()
    {
        startPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void Update()
    {
        if (Vector3.Distance(startPosition, transform.position) >= maxDistance)
            Destroy(gameObject);
    }

    // Stops on the first non-damageable solid thing it hits (walls included), but pierces up to pierceCount damageables
    // before despawning, even though only the attacker's copy deals damage. Trigger-only zones (interaction, etc.) are ignored.
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.isTrigger)
            return;

        var damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null)
        {
            Destroy(gameObject);
            return;
        }

        var behaviour = damageable as MonoBehaviour;
        if (behaviour != null && behaviour.gameObject == owner)
            return;

        if (!hitTargets.Add(damageable))
            return;

        if (hasHitbox)
            damageable.RequestDamage(damage);

        enemiesHit++;
        if (enemiesHit >= pierceCount)
            Destroy(gameObject);
    }
}
