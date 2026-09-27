using UnityEngine;

// Turns a dead player into a roaming pig; polled so late joiners and revives are picked up too.
public class PigForm : MonoBehaviour
{
    private const int StartupGraceFrames = 2;
    private const float TransformFxLifetime = 1f;

    [SerializeField] private GameObject pigPrefab;
    [SerializeField] private float pigScale = 1.5f;
    [SerializeField] private GameObject transformFxPrefab;
    [SerializeField] private Vector2 transformFxOffset = new Vector2(0f, -1.1f);

    private static readonly int IdleHash = Animator.StringToHash("Idle");
    private static readonly int RunHash = Animator.StringToHash("Run");
    private static readonly int ExplosionSpellHash = Animator.StringToHash("Explosion Spell");
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int LastInputXHash = Animator.StringToHash("LastInputX");

    private PlayerStats stats;
    private SpriteRenderer bodyRenderer;
    private Animator bodyAnimator;
    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private int enemyMask;

    private GameObject pig;
    private SpriteRenderer pigRenderer;
    private Animator pigAnimator;
    private bool isPig;
    private bool hasCheckedState;

    private int currentAnimationHash;
    private int startFrame;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
        bodyRenderer = GetComponent<SpriteRenderer>();
        bodyAnimator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        enemyMask = LayerMask.GetMask("Enemy");
    }

    void Start()
    {
        startFrame = Time.frameCount;
    }

    // Health reads 0 until PlayerStats initialises it a frame after spawn, so the first frames are ignored.
    void Update()
    {
        if (stats == null || pigPrefab == null || Time.frameCount < startFrame + StartupGraceFrames)
            return;

        bool dead = stats.IsDead;
        if (dead != isPig)
        {
            SetPig(dead);
            if (dead && hasCheckedState)
                PlayTransformFx();
        }
        hasCheckedState = true;

        if (isPig)
            UpdatePigVisuals();
    }

    // Smoke burst over the body; skipped for players already dead when first seen.
    private void PlayTransformFx()
    {
        if (transformFxPrefab == null)
            return;

        Vector3 center = bodyCollider != null ? bodyCollider.bounds.center : transform.position;
        GameObject fx = Instantiate(transformFxPrefab, center + (Vector3)transformFxOffset, Quaternion.identity);

        SpriteRenderer fxRenderer = fx.GetComponent<SpriteRenderer>();
        fxRenderer.sortingLayerID = bodyRenderer.sortingLayerID;
        fxRenderer.sortingOrder = bodyRenderer.sortingOrder + 1;

        fx.GetComponent<Animator>().Play(ExplosionSpellHash, 0, 0f);
        Destroy(fx, TransformFxLifetime);
    }

    // Dead players walk through enemies instead of blocking or being shoved by them.
    private void SetPig(bool active)
    {
        isPig = active;

        if (active && pig == null)
            CreatePig();

        if (bodyRenderer != null)
            bodyRenderer.enabled = !active;

        if (pig != null)
            pig.SetActive(active);

        if (rb != null)
            rb.excludeLayers = active ? rb.excludeLayers | enemyMask : rb.excludeLayers & ~enemyMask;

        currentAnimationHash = 0;
    }

    private void CreatePig()
    {
        pig = Instantiate(pigPrefab, transform);
        pig.transform.localPosition = Vector3.zero;
        pig.transform.localScale = Vector3.one * pigScale;
        pigRenderer = pig.GetComponent<SpriteRenderer>();
        pigAnimator = pig.GetComponent<Animator>();

        Vector3 bodyCenter = bodyCollider != null ? bodyCollider.bounds.center : transform.position;
        pig.transform.position += bodyCenter - pigRenderer.bounds.center;
    }

    // Reuses the body's own Animator params (already kept in sync across peers by NetworkAnimator) instead of
    // re-deriving movement from a remote-observed Transform, which can read as moving even at rest.
    private void UpdatePigVisuals()
    {
        bool isMoving = bodyAnimator != null && bodyAnimator.GetBool(IsWalkingHash);
        if (bodyAnimator != null)
        {
            float lastInputX = bodyAnimator.GetFloat(LastInputXHash);
            if (Mathf.Abs(lastInputX) > 0.01f)
                pigRenderer.flipX = lastInputX < 0f;
        }

        int desired = isMoving ? RunHash : IdleHash;
        if (desired != currentAnimationHash)
        {
            currentAnimationHash = desired;
            pigAnimator.Play(desired, 0, 0f);
        }

        pigRenderer.sortingLayerID = bodyRenderer.sortingLayerID;
        pigRenderer.sortingOrder = bodyRenderer.sortingOrder;
    }
}
