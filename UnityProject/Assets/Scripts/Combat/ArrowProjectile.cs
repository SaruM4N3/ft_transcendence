using System.Collections.Generic;
using UnityEngine;

// Local one-shot ranged FX, travels independently; kinematic Rigidbody2D avoids tunneling through small colliders at speed.
[RequireComponent(typeof(Rigidbody2D))]
public class ArrowProjectile : MonoBehaviour, IAttackFX
{
    // Optional separate renderer so an animated FX can layer on top of the base sprite instead of replacing it; left unset, frames animate the main sprite.
    [SerializeField] private SpriteRenderer fxSpriteRenderer;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameRate = 24f;

    private GameObject owner;
    private bool hasHitbox;
    private float damage;
    private float speed;
    private float maxDistance;
    private int pierceCount;
    private int enemiesHit;
    private Vector3 startPosition;
    private Rigidbody2D rb;
    private SpriteRenderer animatedRenderer;
    private float frameTimer;
    private int frameIndex;
    private readonly HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

    public void Init(GameObject attacker, bool hasHitboxValue, ClassKit kit, AbilityType ability)
    {
        owner = attacker;
        hasHitbox = hasHitboxValue;
        damage = ability switch
        {
            AbilityType.Special => kit.SpecialDamage,
            AbilityType.Ultimate => kit.UltimateDamage,
            _ => kit.AttackDamage,
        };
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
        animatedRenderer = fxSpriteRenderer != null ? fxSpriteRenderer : GetComponent<SpriteRenderer>();
        if (frames != null && frames.Length > 0)
            animatedRenderer.sprite = frames[0];
    }

    // Optional looping flipbook for projectiles with their own animated frames; a single assigned sprite with no frames array behaves exactly as before.
    void Update()
    {
        if (Vector3.Distance(startPosition, transform.position) >= maxDistance)
        {
            Destroy(gameObject);
            return;
        }

        if (frames == null || frames.Length <= 1)
            return;

        frameTimer += Time.deltaTime;
        float frameDuration = 1f / frameRate;
        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex = (frameIndex + 1) % frames.Length;
            animatedRenderer.sprite = frames[frameIndex];
        }
    }

    // Stops on the first non-damageable solid thing it hits, but pierces up to pierceCount damageables before despawning; trigger-only zones are ignored.
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
