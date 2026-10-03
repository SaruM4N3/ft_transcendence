using UnityEngine;

// Three-stage flipbook (start/loop/end) for a held shield; polls the owner's IsShieldActive instead of a fixed timer so it reacts to both a timeout and the shield breaking early.
public class ShieldHoldFX : MonoBehaviour, IAttackFX, IHealthStats
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] startFrames;
    [SerializeField] private Sprite[] loopFrames;
    [SerializeField] private Sprite[] endFrames;
    [SerializeField] private float frameRate = 24f;
    // Reparented onto the attacker directly in Init so it stays upright above their head instead of orbiting/rotating with the shield sprite.
    [SerializeField] private Transform healthBarRoot;

    private enum Stage { Start, Loop, End }

    private Player owner;
    private Sprite[] currentFrames;
    private Stage stage = Stage.Start;
    private int frameIndex;
    private float frameTimer;
    private float lastReportedHealth = -1f;
    private bool tracksFacing = true;
    private float fxDistance;

    public Player Owner => owner;
    public float CurrentHealth => owner != null ? owner.CurrentShieldHealth : 0f;
    public float MaxHealth => owner != null ? owner.ShieldMaxHealth : 0f;
    public event System.Action<float, float> OnHealthChanged;

    public void Init(GameObject attacker, bool hasHitbox, ClassKit kit, AbilityType ability)
    {
        owner = attacker.GetComponent<Player>();
        if (kit == null || kit.UltimateFollowCaster)
            transform.SetParent(attacker.transform);
        tracksFacing = kit == null || !kit.UltimateIgnoreAimRotation;
        fxDistance = kit != null ? kit.UltimateFxDistance : 0f;
        currentFrames = startFrames;

        if (healthBarRoot != null)
            healthBarRoot.SetParent(attacker.transform, false);

        SortingLayer_Auto attackerSorting = attacker.GetComponent<SortingLayer_Auto>();
        if (attackerSorting != null && spriteRenderer != null)
            spriteRenderer.sortingOrder = attackerSorting.CurrentSortingOrder + 1;
    }

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (startFrames != null && startFrames.Length > 0)
            spriteRenderer.sprite = startFrames[0];
    }

    void Update()
    {
        if (owner == null)
        {
            if (stage != Stage.End)
                EnterEndStage();
        }
        else
        {
            float current = owner.CurrentShieldHealth;
            if (!Mathf.Approximately(current, lastReportedHealth))
            {
                lastReportedHealth = current;
                OnHealthChanged?.Invoke(current, MaxHealth);
            }

            if (stage != Stage.End && !owner.IsShieldActive)
                EnterEndStage();

            if (tracksFacing)
            {
                float angle = owner.ShieldFacingAngle;
                float radians = angle * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
                transform.localPosition = dir * fxDistance;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        AdvanceFrames();
    }

    void OnDestroy()
    {
        if (healthBarRoot != null)
            Destroy(healthBarRoot.gameObject);
    }

    private void EnterEndStage()
    {
        stage = Stage.End;
        currentFrames = endFrames;
        frameIndex = 0;
        frameTimer = 0f;
    }

    private void AdvanceFrames()
    {
        if (currentFrames == null || currentFrames.Length == 0)
        {
            Destroy(gameObject);
            return;
        }

        float frameDuration = 1f / frameRate;
        frameTimer += Time.deltaTime;
        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex++;
            if (frameIndex >= currentFrames.Length)
            {
                if (stage == Stage.Start)
                {
                    stage = Stage.Loop;
                    currentFrames = loopFrames;
                    frameIndex = 0;
                }
                else if (stage == Stage.Loop)
                {
                    frameIndex = 0;
                }
                else
                {
                    Destroy(gameObject);
                    return;
                }
            }

            if (spriteRenderer != null && currentFrames != null && frameIndex < currentFrames.Length)
                spriteRenderer.sprite = currentFrames[frameIndex];
        }
    }
}
