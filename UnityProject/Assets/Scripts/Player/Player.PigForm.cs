using UnityEngine;

public partial class Player
{
    private const int StartupGraceFrames = 2;
    private const float TransformFxLifetime = 1f;

    [Header("Pig Form")]
    [SerializeField] private GameObject pigPrefab;
    [SerializeField] private float pigScale = 1.5f;
    [SerializeField] private GameObject transformFxPrefab;
    [SerializeField] private Vector2 transformFxOffset = new Vector2(0f, -1.1f);

    private static readonly int IdleHash = Animator.StringToHash("Idle");
    private static readonly int RunHash = Animator.StringToHash("Run");
    private static readonly int ExplosionSpellHash = Animator.StringToHash("Explosion Spell");

    private int enemyMask;

    private GameObject pig;
    private SpriteRenderer pigRenderer;
    private Animator pigAnimator;
    private bool isPig;
    private bool hasCheckedState;

    private int currentAnimationHash;
    private int startFrame;

    private void AwakePigForm()
    {
        enemyMask = LayerMask.GetMask("Enemy");
    }

    private void StartPigForm()
    {
        startFrame = Time.frameCount;
    }

    // Health reads 0 until stats initialise it a frame after spawn, so the first frames are ignored.
    private void UpdatePigForm()
    {
        if (pigPrefab == null || Time.frameCount < startFrame + StartupGraceFrames)
            return;

        bool dead = IsDead;
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
        fxRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        fxRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;

        fx.GetComponent<Animator>().Play(ExplosionSpellHash, 0, 0f);
        Destroy(fx, TransformFxLifetime);
    }

    // Dead players walk through enemies instead of blocking or being shoved by them.
    private void SetPig(bool active)
    {
        isPig = active;

        if (active && pig == null)
            CreatePig();

        if (spriteRenderer != null)
            spriteRenderer.enabled = !active;

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

    // Reuses the body's own (NetworkAnimator-synced) params instead of a remote Transform, which can read as moving at rest.
    private void UpdatePigVisuals()
    {
        bool isMoving = animator != null && animator.GetBool(IsWalkingHash);
        if (animator != null)
        {
            float lastInputX = animator.GetFloat(LastInputXHash);
            if (Mathf.Abs(lastInputX) > 0.01f)
                pigRenderer.flipX = lastInputX < 0f;
        }

        int desired = isMoving ? RunHash : IdleHash;
        if (desired != currentAnimationHash)
        {
            currentAnimationHash = desired;
            pigAnimator.Play(desired, 0, 0f);
        }

        pigRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        pigRenderer.sortingOrder = spriteRenderer.sortingOrder;
    }
}
