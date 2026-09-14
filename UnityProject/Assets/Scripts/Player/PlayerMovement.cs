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

    private readonly NetworkVariable<float> aimAngle = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public float AimAngleDegrees => aimAngle.Value;

    private Animator animator;
    private Vector2 moveInput;
    private Vector2 facing = Vector2.down;
    private bool facingApplied;
    private float moveSpeedMult = 1f;
    private bool isGuarding;
    private bool isRunning;

    private float lightAttackReadyTime;
    private float heavyAttackReadyTime;
    private float guardReadyTime;

    private PauseManager pauseManager;
    private PlayerStats stats;

    private bool IsBlocked => PauseManager.IsPaused;

    // Dead players (pigs) can still move but not fight.
    private bool CanFight => !IsBlocked && (stats == null || !stats.IsDead);

    private const float FacingHysteresis = 1.15f;
    private const float BackwardDotThreshold = -0.1f;

    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int IsGuardingHash = Animator.StringToHash("IsGuarding");
    private static readonly int MoveSpeedMultHash = Animator.StringToHash("MoveSpeedMult");
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

        // Covers the offline player, which never goes through OnNetworkSpawn.
        PlayerInput input = GetComponent<PlayerInput>();
        if (input != null)
            KeybindOverrides.Apply(input.actions);
    }

    // Only the owner reads input and renders a camera.
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

        PlayerInput ownerInput = GetComponent<PlayerInput>();
        if (ownerInput != null)
            KeybindOverrides.Apply(ownerInput.actions);

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

        Rigidbody2D body = GetComponent<Rigidbody2D>();
        body.position = spawnPoint.transform.position;
        body.linearVelocity = Vector2.zero;
        transform.position = spawnPoint.transform.position;
    }

    void Update()
    {
        FlipTowards(animator.GetFloat(LastInputXHash));

        if (!this.IsLocallyControlled())
            return;

        if (IsBlocked)
        {
            rb.linearVelocity = Vector2.zero;
            animator.SetBool(IsWalkingHash, false);
            return;
        }

        Vector2 aimDir = GetMouseAimDirection();
        aimAngle.Value = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
        UpdateFacing(aimDir);
        UpdateMoveSpeedMult();

        if (isGuarding && !CanFight)
            StopGuarding();

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

        moveInput = ctx.ReadValue<Vector2>();
        bool hasDirection = moveInput.sqrMagnitude > 0.0001f;

        if (IsBlocked)
            return;

        animator.SetBool(IsWalkingHash, !isGuarding && hasDirection);
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

        isRunning = true;
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

        if (wantsGuard && (!CanFight || Time.time < guardReadyTime))
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
        if (!this.IsLocallyControlled() || !ctx.performed || !CanFight || Time.time < lightAttackReadyTime)
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

    // Spawns the slash FX on every client; only the attacker's copy deals damage.
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

    // Mouse-to-world aim, evaluated at the moment of attack.
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
        if (!this.IsLocallyControlled() || !ctx.performed || !CanFight || Time.time < heavyAttackReadyTime)
            return;

        animator.SetTrigger(HeavyAttackHash);
        heavyAttackReadyTime = Time.time + heavyAttackCooldown;
        OnAbilityUsed?.Invoke(AbilityType.HeavyAttack, heavyAttackCooldown);
    }

    // Drops the guard when it can no longer be held, e.g. on death.
    private void StopGuarding()
    {
        isGuarding = false;
        animator.SetBool(IsGuardingHash, false);
        animator.SetBool(IsWalkingHash, moveInput.sqrMagnitude > 0.01f);
    }

    // Snaps the aim to a cardinal facing, with hysteresis so diagonals don't flicker.
    private void UpdateFacing(Vector2 aim)
    {
        float absX = Mathf.Abs(aim.x);
        float absY = Mathf.Abs(aim.y);
        Vector2 next = facing;

        if (absX > absY * FacingHysteresis)
            next = new Vector2(Mathf.Sign(aim.x), 0f);
        else if (absY > absX * FacingHysteresis)
            next = new Vector2(0f, Mathf.Sign(aim.y));

        if (facingApplied && next == facing)
            return;

        facing = next;
        facingApplied = true;
        animator.SetFloat(LastInputXHash, facing.x);
        animator.SetFloat(LastInputYHash, facing.y);
    }

    // Reverses the walk cycle while moving away from the facing.
    private void UpdateMoveSpeedMult()
    {
        bool hasDirection = moveInput.sqrMagnitude > 0.0001f;
        bool backward = hasDirection && Vector2.Dot(moveInput.normalized, facing) < BackwardDotThreshold;
        float next = backward ? -1f : 1f;

        if (Mathf.Approximately(next, moveSpeedMult))
            return;

        moveSpeedMult = next;
        animator.SetFloat(MoveSpeedMultHash, moveSpeedMult);
    }

    private void FlipTowards(float directionX)
    {
        if (directionX < 0)
            spriteRenderer.flipX = true;
        else if (directionX > 0)
            spriteRenderer.flipX = false;
    }
}
