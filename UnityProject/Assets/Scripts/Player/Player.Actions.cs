using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class Player
{
    public static event Action<AbilityType, float> OnAbilityUsed;

    [Header("Actions")]
    [SerializeField] private ClassKit defaultKit;

    private ClassKit activeKit;

    private bool isAttackHeld;
    private float attackReadyTime;
    private float specialReadyTime;
    private float ultimateReadyTime;
    private float moveLockEndTime;
    private float moveLockSpeedMultiplier;

    public float MovementSpeedMultiplier
    {
        get
        {
            if (isShieldActive.Value)
                return activeKit != null ? activeKit.UltimateMoveSpeedMultiplier : 1f;
            return Time.time < moveLockEndTime ? moveLockSpeedMultiplier : 1f;
        }
    }

    // Dead players (pigs) can still move but not fight.
    private bool CanFight => !IsBlocked && !IsDead;

    // Animator parameter strings stay as-is (LightAttack/HeavyAttack) - only the C#-facing names change.
    private static readonly int AttackHash = Animator.StringToHash("LightAttack");
    private static readonly int SpecialHash = Animator.StringToHash("HeavyAttack");
    private static readonly int UltimateHash = Animator.StringToHash("Ultimate");
    private static readonly int AttackAnimSpeedHash = Animator.StringToHash("AttackAnimSpeed");
    private static readonly int SpecialAnimSpeedHash = Animator.StringToHash("SpecialAnimSpeed");
    private static readonly int UltimateAnimSpeedHash = Animator.StringToHash("UltimateAnimSpeed");

    private void AwakeActions()
    {
        RefreshActiveKit();
        OnClassOrColorChanged += (_, _) => RefreshActiveKit();
    }

    private void RefreshActiveKit()
    {
        ClassKit kit = CharacterCustomizationMenu.Instance != null
            ? CharacterCustomizationMenu.Instance.GetKit(ClassIndex)
            : null;
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

    private void UpdateActions()
    {
        if (isAttackHeld && this.IsLocallyControlled() && CanFight && Time.time >= attackReadyTime)
            FireAttack();
    }

    private void FireAttack()
    {
        animator.SetFloat(AttackAnimSpeedHash, activeKit.AttackAnimSpeed);
        animator.SetTrigger(AttackHash);
        attackReadyTime = Time.time + activeKit.AttackCooldown;
        moveLockEndTime = Time.time + activeKit.AttackMoveLockDuration;
        moveLockSpeedMultiplier = activeKit.AttackMoveSpeedMultiplier;
        OnAbilityUsed?.Invoke(AbilityType.Attack, activeKit.AttackCooldown);
        SpawnAbilityFx(AbilityType.Attack);
    }

    public void Special(InputAction.CallbackContext ctx)
    {
        if (!this.IsLocallyControlled() || !ctx.performed || !CanFight || Time.time < specialReadyTime)
            return;
        if (!TrySpendMana(activeKit.SpecialManaCost))
            return;

        animator.SetFloat(SpecialAnimSpeedHash, activeKit.SpecialAnimSpeed);
        animator.SetTrigger(SpecialHash);
        specialReadyTime = Time.time + activeKit.SpecialCooldown;
        moveLockEndTime = Time.time + activeKit.SpecialMoveLockDuration;
        moveLockSpeedMultiplier = activeKit.SpecialMoveSpeedMultiplier;
        OnAbilityUsed?.Invoke(AbilityType.Special, activeKit.SpecialCooldown);
        SpawnAbilityFx(AbilityType.Special);
    }

    public void Ultimate(InputAction.CallbackContext ctx)
    {
        if (activeKit != null && activeKit.HasShield)
        {
            UltimateShield(ctx);
            return;
        }

        if (!this.IsLocallyControlled() || !ctx.performed || !CanFight || Time.time < ultimateReadyTime)
            return;
        if (!TrySpendMana(activeKit.UltimateManaCost))
            return;

        animator.SetFloat(UltimateAnimSpeedHash, activeKit.UltimateAnimSpeed);
        animator.SetTrigger(UltimateHash);
        ultimateReadyTime = Time.time + activeKit.UltimateCooldown;
        moveLockEndTime = Time.time + activeKit.UltimateMoveLockDuration;
        moveLockSpeedMultiplier = activeKit.UltimateMoveSpeedMultiplier;
        OnAbilityUsed?.Invoke(AbilityType.Ultimate, activeKit.UltimateCooldown);
        SpawnAbilityFx(AbilityType.Ultimate);
    }

    private GameObject GetFxPrefab(AbilityType ability) => ability switch
    {
        AbilityType.Special => activeKit.SpecialFxPrefab,
        AbilityType.Ultimate => activeKit.UltimateFxPrefab,
        _ => activeKit.AttackFxPrefab,
    };

    private float GetFxDistance(AbilityType ability) => ability switch
    {
        AbilityType.Special => activeKit.SpecialFxDistance,
        AbilityType.Ultimate => activeKit.UltimateFxDistance,
        _ => activeKit.AttackFxDistance,
    };

    private float GetSize(AbilityType ability) => ability switch
    {
        AbilityType.Special => activeKit.SpecialSize,
        AbilityType.Ultimate => activeKit.UltimateSize,
        _ => activeKit.AttackSize,
    };

    private bool GetIgnoreAimRotation(AbilityType ability) => ability switch
    {
        AbilityType.Special => activeKit.SpecialIgnoreAimRotation,
        AbilityType.Ultimate => activeKit.UltimateIgnoreAimRotation,
        _ => activeKit.AttackIgnoreAimRotation,
    };

    private void SpawnAbilityFx(AbilityType ability)
    {
        if (GetFxPrefab(ability) == null)
            return;

        float angle = AimAngleDegrees;

        if (NetworkObject.IsSpawned)
            SpawnAbilityFxServerRpc(angle, ability);
        else
            SpawnLocalFx(angle, hasHitbox: true, ability);
    }

    [ServerRpc]
    private void SpawnAbilityFxServerRpc(float angle, AbilityType ability)
    {
        SpawnAbilityFxClientRpc(angle, OwnerClientId, ability);
    }

    // Spawns the ability FX on every client; only the attacker's copy deals damage.
    [ClientRpc]
    private void SpawnAbilityFxClientRpc(float angle, ulong attackerClientId, AbilityType ability)
    {
        bool hasHitbox = NetworkManager.Singleton.LocalClientId == attackerClientId;
        SpawnLocalFx(angle, hasHitbox, ability);
    }

    // Position is computed locally from the attacker's current transform, not sent over RPC - round-trip latency would otherwise bake a stale offset into the spawn point.
    private void SpawnLocalFx(float angle, bool hasHitbox, AbilityType ability)
    {
        GameObject prefab = GetFxPrefab(ability);
        if (prefab == null)
            return;

        Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        Vector3 spawnPos = transform.position + (Vector3)(dir * GetFxDistance(ability));
        Quaternion rotation = GetIgnoreAimRotation(ability) ? Quaternion.identity : Quaternion.Euler(0f, 0f, angle);

        GameObject fx = Instantiate(prefab, spawnPos, rotation);
        fx.transform.localScale *= GetSize(ability);
        fx.GetComponent<IAttackFX>()?.Init(gameObject, hasHitbox, activeKit, ability);
    }
}
