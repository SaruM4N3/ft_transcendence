using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public partial class Player
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float runSpeed = 10f;

    private Camera cam;

    private readonly NetworkVariable<float> aimAngle = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public float AimAngleDegrees => aimAngle.Value;
    public bool HasMoveInput => moveInput.sqrMagnitude > 0.01f;

    private Vector2 moveInput;
    private Vector2 facing = Vector2.down;
    private bool facingApplied;
    private float moveSpeedMult = 1f;
    private bool isRunning;

    private PauseManager pauseManager;

    private const float FacingHysteresis = 1.15f;
    private const float BackwardDotThreshold = -0.1f;

    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int MoveSpeedMultHash = Animator.StringToHash("MoveSpeedMult");
    private static readonly int LastInputXHash = Animator.StringToHash("LastInputX");
    private static readonly int LastInputYHash = Animator.StringToHash("LastInputY");

    // Also applies keybinds here, covering the offline player which never goes through OnNetworkSpawn.
    private void StartMovement()
    {
        if (playerInput != null)
            KeybindOverrides.Apply(playerInput.actions);
    }

    // Only the owner reads input and renders a camera.
    private void SpawnMovement()
    {
        if (!IsOwner)
        {
            if (playerInput != null)
                playerInput.enabled = false;

            PlayerCameraRig.SetActive(transform, active: false);
            return;
        }

        if (playerInput != null)
            KeybindOverrides.Apply(playerInput.actions);

        if (NetworkManager.SceneManager != null)
            NetworkManager.SceneManager.OnLoadComplete += HandleSceneLoadComplete;
    }

    private void DespawnMovement()
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

        rb.position = spawnPoint.transform.position;
        rb.linearVelocity = Vector2.zero;
        transform.position = spawnPoint.transform.position;
    }

    private void UpdateMovement()
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

        // Dead (pig form): face where it's walking, not the mouse - there's nothing left to aim.
        Vector2 aimDir = IsDead ? GetMovementFacingDirection() : GetMouseAimDirection();
        aimAngle.Value = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
        UpdateFacing(aimDir);
        UpdateMoveSpeedMult();

        StopGuardingIfCannotFight();

        // Mounted on a pig to revive it: can still aim/attack, but UpdateMount drives position instead.
        if (isMounted)
        {
            rb.linearVelocity = Vector2.zero;
            animator.SetBool(IsWalkingHash, false);
            return;
        }

        float speedMult = MovementSpeedMultiplier;
        if (speedMult <= 0f)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = moveInput * (isRunning ? runSpeed : moveSpeed) * speedMult;
    }

    public void Move(InputAction.CallbackContext ctx)
    {
        if (!this.IsLocallyControlled())
            return;

        moveInput = ctx.ReadValue<Vector2>();
        bool hasDirection = moveInput.sqrMagnitude > 0.0001f;

        if (IsBlocked)
            return;

        animator.SetBool(IsWalkingHash, !IsGuarding && hasDirection);
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

    private Vector2 GetMovementFacingDirection()
    {
        return moveInput.sqrMagnitude > 0.0001f ? moveInput.normalized : facing;
    }

    // Mouse-to-world aim, evaluated every frame.
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
