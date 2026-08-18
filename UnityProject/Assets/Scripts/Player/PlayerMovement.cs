using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : NetworkBehaviour
{
    public static event Action<AbilityType, float> OnAbilityUsed;

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float runSpeed = 10f;
    [SerializeField] private float lightAttackCooldown = 0.5f;
    [SerializeField] private float heavyAttackCooldown = 1f;
    [SerializeField] private float guardCooldown = 0.5f;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;

    private Animator animator;
    private Vector2 moveInput;
    private Vector2 facing = Vector2.down;
    private bool isGuarding;
    private bool isRunning;

    private float lightAttackReadyTime;
    private float heavyAttackReadyTime;
    private float guardReadyTime;

    // PauseManager lives on its own GameObject in the scene, not the player prefab, so it's resolved
    // lazily instead of serialized.
    private PauseManager pauseManager;

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
    }

    // Only the owner's copy should read local input or render a camera - every other copy is a
    // remote puppet driven by NetworkTransform/NetworkAnimator.
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
            return;

        PlayerInput input = GetComponent<PlayerInput>();
        if (input != null)
            input.enabled = false;

        Transform mainCamera = transform.Find("Main Camera");
        if (mainCamera != null)
            mainCamera.gameObject.SetActive(false);

        Transform cinemachineCamera = transform.Find("CinemachineCamera");
        if (cinemachineCamera != null)
            cinemachineCamera.gameObject.SetActive(false);
    }

    void Update()
    {
        // LastInputX is a NetworkAnimator-synced parameter, so this keeps a remote puppet's sprite
        // mirrored to match its owner's facing too - flipX itself isn't a networked value.
        FlipTowards(animator.GetFloat(LastInputXHash));

        if (!this.IsLocallyControlled())
            return;

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
        if (!this.IsLocallyControlled() || PauseManager.IsPaused)
            return;

        moveInput = ctx.ReadValue<Vector2>();
        bool hasDirection = moveInput.sqrMagnitude > 0.0001f;

        animator.SetBool(IsWalkingHash, !isGuarding && hasDirection);
        animator.SetFloat(InputXHash, moveInput.x);
        animator.SetFloat(InputYHash, moveInput.y);

        if (hasDirection)
            UpdateFacing(moveInput);
    }

    public void Run(InputAction.CallbackContext ctx)
    {
        if (!this.IsLocallyControlled() || PauseManager.IsPaused)
            return;
        if (ctx.canceled)
        {
            isRunning = false;
            return;
        }
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
        if (!this.IsLocallyControlled() || PauseManager.IsPaused)
            return;

        bool wantsGuard = !ctx.canceled;

        if (wantsGuard && Time.time < guardReadyTime)
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
        if (!this.IsLocallyControlled() || !ctx.performed || PauseManager.IsPaused || Time.time < lightAttackReadyTime)
            return;

        animator.SetTrigger(LightAttackHash);
        lightAttackReadyTime = Time.time + lightAttackCooldown;
        OnAbilityUsed?.Invoke(AbilityType.LightAttack, lightAttackCooldown);
    }

    public void HeavyAttack(InputAction.CallbackContext ctx)
    {
        if (!this.IsLocallyControlled() || !ctx.performed || PauseManager.IsPaused || Time.time < heavyAttackReadyTime)
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
        // Sprite flip itself is applied uniformly (owner and remote puppets alike) from Update(),
        // reading this same LastInputX value back off the (NetworkAnimator-synced) Animator.
    }

    private void FlipTowards(float directionX)
    {
        if (directionX < 0)
            spriteRenderer.flipX = true;
        else if (directionX > 0)
            spriteRenderer.flipX = false;
    }
}
