using System.Collections.Generic;
using UnityEngine;

// One-shot flipbook FX, spawned locally (not networked itself) on every client so an attack is
// visible to everyone; only the attacker's own copy keeps a live hitbox (see PlayerMovement).
public class SlashAttackFX : MonoBehaviour
{
    public static event System.Action<GameObject, Vector3> OnHitLanded;

    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameRate = 24f;
    [SerializeField] private float damage = 15f;

    private SpriteRenderer spriteRenderer;
    private Collider2D hitbox;
    private GameObject owner;
    private readonly HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();
    private int frameIndex;
    private float frameTimer;

    public void Init(GameObject attacker, bool hasHitbox)
    {
        owner = attacker;
        if (!hasHitbox && hitbox != null)
            hitbox.enabled = false;
    }

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        hitbox = GetComponent<Collider2D>();
        if (frames.Length > 0)
            spriteRenderer.sprite = frames[0];
    }

    void Update()
    {
        float frameDuration = 1f / frameRate;
        frameTimer += Time.deltaTime;

        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex++;
            if (frameIndex >= frames.Length)
            {
                Destroy(gameObject);
                return;
            }
            spriteRenderer.sprite = frames[frameIndex];
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null || hitTargets.Contains(damageable))
            return;

        var behaviour = damageable as MonoBehaviour;
        if (behaviour != null && behaviour.gameObject == owner)
            return;

        hitTargets.Add(damageable);
        damageable.RequestDamage(damage);
        OnHitLanded?.Invoke(owner, other.ClosestPoint(transform.position));
    }
}
