using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerMovement : NetworkBehaviour
{
    public static event Action<AbilityType, float> OnAbilityUsed;

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float runSpeed = 10f;
    [SerializeField] private float lightAttackCooldown = 0.5f;
    [SerializeField] private float heavyAttackCooldown = 1f;
    [SerializeField] private float guardCooldown = 0.5f;

    [SerializeField] private GameObject lightAttackFxPrefab;
    [SerializeField] private float lightAttackFxDistance = 1f;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Camera cam;

    // Synced so remote clients can orient the direction indicator and this player's attack FX
    // the same way the owner sees them, not just replicate position/animation.
    private readonly NetworkVariable<float> aimAngle = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public float AimAngleDegrees => aimAngle.Value;

    private Animator animator;
    private Vector2 moveInput;
    private Vector2 facing = Vector2.down;
    private bool isGuarding;
    private bool isRunning;

    private float lightAttackReadyTime;
    private float heavyAttackReadyTime;
    private float guardReadyTime;

    // Not serialized - PauseManager lives on its own scene GameObject, not the player prefab.
    private PauseManager pauseManager;
    private PlayerStats stats;

    // Movement/actions freeze the same way for a pause as for death; a dead player also disappears
    // visually (OnHealthReplicated fires for every observer, not just the local owner).
    private bool IsBlocked => PauseManager.IsPaused || (stats != null && stats.IsDead);

    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int IsGuardingHash = Animator.StringToHash("IsGuarding");
    private static readonly int InputXHash = Animator.StringToHash("InputX");
    private static readonly int InputYHash = Animator.StringToHash("InputY");
    private static readonly int LastInputXHash = Animator.StringToHash("LastInputX");
    private static readonly int LastInputYHash = Animator.StringToHash("LastInputY");
    private static readonly int LightAttackHash = Animator.StringToHash("LightAttack");
    private static readonly int HeavyAttackHash = Animator.StringToHash("HeavyAttack");

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        stats = GetComponent<PlayerStats>();
        if (stats != null)
            stats.OnHealthReplicated += HandleHealthReplicated;
    }

    void OnDestroy()
    {
        if (stats != null)
            stats.OnHealthReplicated -= HandleHealthReplicated;
    }

    // Fires for every observer (not just the local owner), so a player dying is visible to everyone.
    private void HandleHealthReplicated(float currentHealth, float maxHealth)
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = currentHealth > 0f;
    }

    // Only the owner reads local input or renders a camera - other copies are remote puppets.
    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            PlayerInput input = GetComponent<PlayerInput>();
            if (input != null)
                input.enabled = false;

            PlayerCameraRig.SetActive(transform, active: false);
            return;
        }

        // Player NetworkObjects persist across scene loads (DestroyWithScene = false), so without this
        // everyone would land wherever they stood in the previous scene instead of together.
        if (NetworkManager.SceneManager != null)
            NetworkManager.SceneManager.OnLoadComplete += HandleSceneLoadComplete;
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && NetworkManager != null && NetworkManager.SceneManager != null)
            NetworkManager.SceneManager.OnLoadComplete -= HandleSceneLoadComplete;
    }

    private void HandleSceneLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        if (NetworkManager == null || clientId != NetworkManager.LocalClientId)
            return;

        PlayerSpawnPoint spawnPoint = FindAnyObjectByType<PlayerSpawnPoint>();
        if (spawnPoint == null)
            return;

        // Not the cached rb field - this can fire before Start() assigns it.
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        body.position = spawnPoint.transform.position;
        body.linearVelocity = Vector2.zero;
        transform.position = spawnPoint.transform.position;
    }

    void Update()
    {
        // LastInputX is NetworkAnimator-synced, so this mirrors a remote puppet's sprite too - flipX itself isn't.
        FlipTowards(animator.GetFloat(LastInputXHash));

        if (!this.IsLocallyControlled())
            return;

        // Freezes movement while paused or dead - Move/Run/Guard still track real input so state is correct on unpause.
        if (IsBlocked)
        {
            rb.linearVelocity = Vector2.zero;
            animator.SetBool(IsWalkingHash, false);
            return;
        }

        Vector2 aimDir = GetMouseAimDirection();
        aimAngle.Value = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;

        if (isGuarding)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (isRunning)
            rb.linearVelocity = moveInput * runSpeed;
        else
            rb.linearVelocity = moveInput * moveSpeed;
    }

    public void Move(InputAction.CallbackContext ctx)
    {
        if (!this.IsLocallyControlled())
            return;

        // Tracks input even while paused, so a key released mid-pause doesn't leave the player sliding after unpause.
        moveInput = ctx.ReadValue<Vector2>();
        bool hasDirection = moveInput.sqrMagnitude > 0.0001f;

        if (IsBlocked)
            return;

        animator.SetBool(IsWalkingHash, !isGuarding && hasDirection);
        animator.SetFloat(InputXHash, moveInput.x);
        animator.SetFloat(InputYHash, moveInput.y);

        if (hasDirection)
            UpdateFacing(moveInput);
    }

    public void Run(InputAction.CallbackContext ctx)
    {
        if (!this.IsLocallyControlled())
            return;
        if (ctx.canceled)
        {
            isRunning = false;
            return;
        }
        if (IsBlocked)
            return;

        bool hasDirection = moveInput.sqrMagnitude > 0.0001f;
        isRunning = true;
        if (hasDirection)
            UpdateFacing(moveInput);
    }

    public void OnPause(InputAction.CallbackContext ctx)
    {
        if (!this.IsLocallyControlled())
            return;

        if (pauseManager == null)
            pauseManager = FindAnyObjectByType<PauseManager>();

        pauseManager?.Pause(ctx);
    }

    public void Guard(InputAction.CallbackContext ctx)
    {
        if (!this.IsLocallyControlled())
            return;

        bool wantsGuard = !ctx.canceled;

        // Releasing guard must work even while paused, or isGuarding stays stuck true and freezes the player.
        if (wantsGuard && (IsBlocked || Time.time < guardReadyTime))
            return;

        if (ctx.canceled && !isGuarding)
            return;

        isGuarding = wantsGuard;

        animator.SetBool(IsGuardingHash, isGuarding);
        animator.SetBool(IsWalkingHash, !isGuarding && moveInput.sqrMagnitude > 0.01f);

        if (ctx.canceled)
        {
            guardReadyTime = Time.time + guardCooldown;
            OnAbilityUsed?.Invoke(AbilityType.Guard, guardCooldown);
        }
    }

    public void LightAttack(InputAction.CallbackContext ctx)
    {
        if (!this.IsLocallyControlled() || !ctx.performed || IsBlocked || Time.time < lightAttackReadyTime)
            return;

        animator.SetTrigger(LightAttackHash);
        lightAttackReadyTime = Time.time + lightAttackCooldown;
        OnAbilityUsed?.Invoke(AbilityType.LightAttack, lightAttackCooldown);
        SpawnLightAttackFx();
    }

    private void SpawnLightAttackFx()
    {
        if (lightAttackFxPrefab == null)
            return;

        float angle = aimAngle.Value;
        Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        Vector3 spawnPos = transform.position + (Vector3)(dir * lightAttackFxDistance);

        if (NetworkObject.IsSpawned)
            SpawnLightAttackFxServerRpc(spawnPos, angle);
        else
            SpawnLocalFx(spawnPos, angle, hasHitbox: true);
    }

    [ServerRpc]
    private void SpawnLightAttackFxServerRpc(Vector3 position, float angle)
    {
        SpawnLightAttackFxClientRpc(position, angle, OwnerClientId);
    }

    // Every client spawns the same cosmetic flipbook so an attack is visible to everyone, but only
    // the attacker's own copy keeps its hitbox - otherwise every client would independently deal damage.
    [ClientRpc]
    private void SpawnLightAttackFxClientRpc(Vector3 position, float angle, ulong attackerClientId)
    {
        bool hasHitbox = NetworkManager.Singleton.LocalClientId == attackerClientId;
        SpawnLocalFx(position, angle, hasHitbox);
    }

    private void SpawnLocalFx(Vector3 position, float angle, bool hasHitbox)
    {
        GameObject fx = Instantiate(lightAttackFxPrefab, position, Quaternion.Euler(0f, 0f, angle));
        fx.GetComponent<SlashAttackFX>()?.Init(gameObject, hasHitbox);
    }

    // Same mouse-to-world math as MouseDirectionIndicator, kept local since the FX's rotation
    // needs it at the moment of attack rather than every frame.
    private Vector2 GetMouseAimDirection()
    {
        if (cam == null)
            cam = Camera.main;
        if (cam == null || Mouse.current == null)
            return facing;

        Vector3 mouseScreen = Mouse.current.position.ReadValue();
        mouseScreen.z = -cam.transform.position.z;
        Vector3 mouseWorld = cam.ScreenToWorldPoint(mouseScreen);

        Vector2 dir = (Vector2)mouseWorld - (Vector2)transform.position;
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : facing;
    }

    public void HeavyAttack(InputAction.CallbackContext ctx)
    {
        if (!this.IsLocallyControlled() || !ctx.performed || IsBlocked || Time.time < heavyAttackReadyTime)
            return;

        animator.SetTrigger(HeavyAttackHash);
        heavyAttackReadyTime = Time.time + heavyAttackCooldown;
        OnAbilityUsed?.Invoke(AbilityType.HeavyAttack, heavyAttackCooldown);
    }

    private void UpdateFacing(Vector2 direction)
    {
        facing = direction;
        animator.SetFloat(LastInputXHash, facing.x);
        animator.SetFloat(LastInputYHash, facing.y);
    }

    private void FlipTowards(float directionX)
    {
        if (directionX < 0)
            spriteRenderer.flipX = true;
        else if (directionX > 0)
            spriteRenderer.flipX = false;
    }
}
