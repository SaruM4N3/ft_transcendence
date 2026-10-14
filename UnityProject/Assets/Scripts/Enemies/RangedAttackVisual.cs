using UnityEngine;

// Enemy's thrown shot; the prefab carries its own SpriteRenderer/frames/Rigidbody2D/CircleCollider2D (same shape as the player's ArrowProjectile), Init supplies the per-kit speed/range/damage. ResolveHit's windup timer still decides whether the shot would connect (range/angle re-check); this is what actually detects a shield in its path - a real trigger hitbox against the shield's collider, not a position/radius check, so it works regardless of how far the shooter is from the shield.
[RequireComponent(typeof(Rigidbody2D))]
public class RangedAttackVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameRate = 16f;

    private Rigidbody2D rb;
    private float damage;
    private float maxDistance;
    private float arcHeight;
    private bool isAuthoritative;
    private System.Action onBlocked;
    private bool resolved;
    private float frameTimer;
    private int frameIndex;
    private Vector3 startPosition;
    private Vector3 spriteBaseLocalPosition;

    // isAuthoritative should be true only for the server/offline-authoritative spawn (see EnemyActions.HasServerAuthority) - every other peer's copy is replay-only and must not call the ClientRpc-backed ApplyShieldDamage.
    public static void Show(GameObject prefab, Vector3 from, Vector2 direction, float speed, float maxDistance, float arcHeight, float damage, bool isAuthoritative, System.Action onBlocked = null)
    {
        if (prefab == null || speed <= 0f || maxDistance <= 0f || direction.sqrMagnitude < 0.0001f)
            return;

        GameObject go = Instantiate(prefab, from, Quaternion.identity);
        RangedAttackVisual visual = go.GetComponent<RangedAttackVisual>();
        if (visual == null)
        {
            Destroy(go);
            return;
        }

        visual.Init(direction.normalized * speed, maxDistance, arcHeight, damage, isAuthoritative, onBlocked);
    }

    private void Init(Vector2 velocity, float maxDist, float arc, float damageValue, bool authoritative, System.Action onBlockedCallback)
    {
        maxDistance = maxDist;
        arcHeight = arc;
        damage = damageValue;
        isAuthoritative = authoritative;
        onBlocked = onBlockedCallback;
        startPosition = transform.position;
        rb.linearVelocity = velocity;
    }

    // Re-asserts the physics setup this needs regardless of what the prefab's Inspector values happen to be, since a kinematic+continuous+trigger setup is load-bearing for hitting the shield.
    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (frames != null && frames.Length > 0 && spriteRenderer != null)
            spriteRenderer.sprite = frames[0];
        if (spriteRenderer != null)
            spriteBaseLocalPosition = spriteRenderer.transform.localPosition;

        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.gravityScale = 0f;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    // Travels via the Rigidbody2D's own velocity (set once in Init) instead of a per-frame Transform/MovePosition write, matching ArrowProjectile; the arc lob is a visual-only offset on the child sprite so it doesn't disturb the physics path the hitbox travels along.
    void Update()
    {
        float traveled = Vector3.Distance(startPosition, transform.position);
        if (traveled >= maxDistance)
        {
            Destroy(gameObject);
            return;
        }

        if (spriteRenderer != null && arcHeight > 0f)
        {
            float fraction = Mathf.Clamp01(traveled / maxDistance);
            Vector3 offset = spriteBaseLocalPosition;
            offset.y += Mathf.Sin(fraction * Mathf.PI) * arcHeight;
            spriteRenderer.transform.localPosition = offset;
        }

        if (frames == null || frames.Length <= 1 || spriteRenderer == null)
            return;

        frameTimer += Time.deltaTime;
        float frameDuration = 1f / frameRate;
        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex = (frameIndex + 1) % frames.Length;
            spriteRenderer.sprite = frames[frameIndex];
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (resolved)
            return;

        ShieldHoldFX shield = other.GetComponent<ShieldHoldFX>();
        if (shield == null)
            return;

        resolved = true;
        if (isAuthoritative)
        {
            onBlocked?.Invoke();
            if (shield.Owner != null)
                shield.Owner.ApplyShieldDamage(damage);
        }
        Destroy(gameObject);
    }
}
