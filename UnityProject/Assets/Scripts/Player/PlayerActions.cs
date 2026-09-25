using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// Attack/Special/Ultimate: cooldowns, FX spawning, guard state, all driven by the active ClassKit.
public class PlayerActions : NetworkBehaviour
{
    public static event Action<AbilityType, float> OnAbilityUsed;

    [SerializeField] private ClassKit defaultKit;

    private Animator animator;
    private PlayerMovement movement;
    private PlayerCustomization customization;
    private PlayerStats stats;
    private ClassKit activeKit;

    private bool isGuarding;
    private bool isAttackHeld;
    private float attackReadyTime;
    private float specialReadyTime;
    private float ultimateReadyTime;
    private float attackMoveLockEndTime;

    public bool IsGuarding => isGuarding;

    public float MovementSpeedMultiplier
    {
        get
        {
            if (isGuarding)
                return 0f;
            return Time.time < attackMoveLockEndTime ? activeKit.AttackMoveSpeedMultiplier : 1f;
        }
    }

    private bool IsBlocked => PauseManager.IsPaused;

    // Dead players (pigs) can still move but not fight.
    private bool CanFight => !IsBlocked && (stats == null || !stats.IsDead);

    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int IsGuardingHash = Animator.StringToHash("IsGuarding");
    // Animator parameter strings stay as-is (LightAttack/HeavyAttack) - only the C#-facing names change.
    private static readonly int AttackHash = Animator.StringToHash("LightAttack");
    private static readonly int SpecialHash = Animator.StringToHash("HeavyAttack");
    private static readonly int AttackAnimSpeedHash = Animator.StringToHash("AttackAnimSpeed");
    private static readonly int SpecialAnimSpeedHash = Animator.StringToHash("SpecialAnimSpeed");

    void Awake()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<PlayerMovement>();
        customization = GetComponent<PlayerCustomization>();
        stats = GetComponent<PlayerStats>();

        RefreshActiveKit();
        if (customization != null)
            customization.OnClassOrColorChanged += (_, _) => RefreshActiveKit();
    }

    private void RefreshActiveKit()
    {
        ClassKit kit = null;
        if (customization != null && CharacterCustomizationMenu.Instance != null)
            kit = CharacterCustomizationMenu.Instance.GetKit(customization.ClassIndex);
        activeKit = kit != null ? kit : defaultKit;
    }

    // Held down: keeps firing on its own once each cooldown ends, instead of requiring repeated clicks.
    public void Attack(InputAction.CallbackContext ctx)
    {
        if (!this.IsLocallyControlled())
            return;

        if (ctx.canceled)
            isAttackHeld = false;
        else if (ctx.performed)
            isAttackHeld = true;
    }

    void Update()
    {
        if (isAttackHeld && this.IsLocallyControlled() && CanFight && Time.time >= attackReadyTime)
            FireAttack();
    }

    private void FireAttack()
    {
        animator.SetFloat(AttackAnimSpeedHash, activeKit.AttackAnimSpeed);
        animator.SetTrigger(AttackHash);
        attackReadyTime = Time.time + activeKit.AttackCooldown;
        attackMoveLockEndTime = Time.time + activeKit.AttackMoveLockDuration;
        OnAbilityUsed?.Invoke(AbilityType.Attack, activeKit.AttackCooldown);
        SpawnAttackFx();
    }

    public void Special(InputAction.CallbackContext ctx)
    {
        if (!this.IsLocallyControlled() || !ctx.performed || !CanFight || Time.time < specialReadyTime)
            return;

        animator.SetFloat(SpecialAnimSpeedHash, activeKit.SpecialAnimSpeed);
        animator.SetTrigger(SpecialHash);
        specialReadyTime = Time.time + activeKit.SpecialCooldown;
        attackMoveLockEndTime = Time.time + activeKit.AttackMoveLockDuration;
        OnAbilityUsed?.Invoke(AbilityType.Special, activeKit.SpecialCooldown);
    }

    public void Ultimate(InputAction.CallbackContext ctx)
    {
        if (!this.IsLocallyControlled())
            return;

        bool wantsGuard = !ctx.canceled;

        if (wantsGuard && (!CanFight || Time.time < ultimateReadyTime))
            return;

        if (ctx.canceled && !isGuarding)
            return;

        isGuarding = wantsGuard;

        animator.SetBool(IsGuardingHash, isGuarding);
        animator.SetBool(IsWalkingHash, !isGuarding && movement != null && movement.HasMoveInput);

        if (ctx.canceled)
        {
            ultimateReadyTime = Time.time + activeKit.UltimateCooldown;
            OnAbilityUsed?.Invoke(AbilityType.Ultimate, activeKit.UltimateCooldown);
        }
    }

    // Drops the guard when it can no longer be held, e.g. on death.
    public void StopGuardingIfCannotFight()
    {
        if (!isGuarding || CanFight)
            return;

        isGuarding = false;
        animator.SetBool(IsGuardingHash, false);
        animator.SetBool(IsWalkingHash, movement != null && movement.HasMoveInput);
    }

    private void SpawnAttackFx()
    {
        if (activeKit.AttackFxPrefab == null)
            return;

        float angle = movement.AimAngleDegrees;
        Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        Vector3 spawnPos = transform.position + (Vector3)(dir * activeKit.AttackFxDistance);

        if (NetworkObject.IsSpawned)
            SpawnAttackFxServerRpc(spawnPos, angle);
        else
            SpawnLocalFx(spawnPos, angle, hasHitbox: true);
    }

    [ServerRpc]
    private void SpawnAttackFxServerRpc(Vector3 position, float angle)
    {
        SpawnAttackFxClientRpc(position, angle, OwnerClientId);
    }

    // Spawns the attack FX on every client; only the attacker's copy deals damage.
    [ClientRpc]
    private void SpawnAttackFxClientRpc(Vector3 position, float angle, ulong attackerClientId)
    {
        bool hasHitbox = NetworkManager.Singleton.LocalClientId == attackerClientId;
        SpawnLocalFx(position, angle, hasHitbox);
    }

    // Not parented here - each FX type decides that for itself (a slash tracks the attacker, a projectile doesn't).
    private void SpawnLocalFx(Vector3 position, float angle, bool hasHitbox)
    {
        GameObject prefab = activeKit.AttackFxPrefab;
        if (prefab == null)
            return;

        GameObject fx = Instantiate(prefab, position, Quaternion.Euler(0f, 0f, angle));
        fx.GetComponent<IAttackFX>()?.Init(gameObject, hasHitbox);
    }
}
