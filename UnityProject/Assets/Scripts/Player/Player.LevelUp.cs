using Unity.Netcode;
using UnityEngine;

public partial class Player
{
    private readonly NetworkVariable<bool> hasChosenLevelUpBonus = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<LevelUpBonusStacks> levelUpBonusStacks = new NetworkVariable<LevelUpBonusStacks>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public bool HasChosenLevelUpBonus => hasChosenLevelUpBonus.Value;

    public float AttackSpeedMultiplier =>
        1f + levelUpBonusStacks.Value.attackSpeed * LevelUpBonusCatalog.AttackCooldownReductionPerStack;
    public float AttackCooldownMultiplier => 1f / AttackSpeedMultiplier;
    public float SpecialCooldownMultiplier =>
        1f / (1f + levelUpBonusStacks.Value.specialCooldown * LevelUpBonusCatalog.SpecialCooldownReductionPerStack);
    public float DamageMultiplier =>
        1f + levelUpBonusStacks.Value.damage * LevelUpBonusCatalog.DamageBonusPerStack;
    public float LevelUpHealthBonus =>
        levelUpBonusStacks.Value.health * LevelUpBonusCatalog.HealthBonusPerStack;
    public float MoveSpeedBonusMultiplier =>
        1f + levelUpBonusStacks.Value.moveSpeed * LevelUpBonusCatalog.MoveSpeedBonusPerStack;
    public float LevelUpHealthRegenBonus =>
        levelUpBonusStacks.Value.healthRegen * LevelUpBonusCatalog.HealthRegenBonusPerStack;
    public float LevelUpManaBonus =>
        levelUpBonusStacks.Value.manaMax * LevelUpBonusCatalog.ManaBonusPerStack;
    public float LevelUpManaRegenBonus =>
        levelUpBonusStacks.Value.manaRegen * LevelUpBonusCatalog.ManaRegenBonusPerStack;
    public float SizeBonusMultiplier =>
        1f + levelUpBonusStacks.Value.size * LevelUpBonusCatalog.SizeBonusPerStack;
    public float AttackMobilityBonus =>
        levelUpBonusStacks.Value.attackMobility * LevelUpBonusCatalog.AttackMobilityBonusPerStack;

    // Raises a move-lock speed fraction toward 1 (no slowdown), never past it.
    private float ApplyAttackMobilityBonus(float baseMultiplier) => Mathf.Min(1f, baseMultiplier + AttackMobilityBonus);

    // Called by this player's own level-up UI.
    public void ChooseLevelUpBonus(LevelUpBonusType type)
    {
        if (!this.IsLocallyControlled())
            return;

        ChooseLevelUpBonusLocal(type);
    }

    // Called by LevelUpManager (server) on a player that didn't pick before the timer ran out.
    public void ForceLevelUpBonus(LevelUpBonusType type)
    {
        if (!NetworkObject.IsSpawned)
        {
            ChooseLevelUpBonusLocal(type);
            return;
        }
        ForceLevelUpBonusClientRpc(type, new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
        });
    }

    // Called by LevelUpManager (server) when a new level-up sequence starts.
    public void ResetLevelUpChoice()
    {
        if (!NetworkObject.IsSpawned)
        {
            ResetLevelUpChoiceLocal();
            return;
        }
        ResetLevelUpChoiceClientRpc(new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
        });
    }

    [ClientRpc]
    private void ForceLevelUpBonusClientRpc(LevelUpBonusType type, ClientRpcParams rpcParams = default)
    {
        ChooseLevelUpBonusLocal(type);
    }

    [ClientRpc]
    private void ResetLevelUpChoiceClientRpc(ClientRpcParams rpcParams = default)
    {
        ResetLevelUpChoiceLocal();
    }

    private void ChooseLevelUpBonusLocal(LevelUpBonusType type)
    {
        hasChosenLevelUpBonus.Value = true;
        LevelUpBonusStacks stacks = levelUpBonusStacks.Value;
        stacks.Increment(type);
        levelUpBonusStacks.Value = stacks;
        if (type == LevelUpBonusType.Health)
            Heal(LevelUpBonusCatalog.HealthBonusPerStack);
        else if (type == LevelUpBonusType.ManaMax)
            RestoreMana(LevelUpBonusCatalog.ManaBonusPerStack);
    }

    private void ResetLevelUpChoiceLocal()
    {
        hasChosenLevelUpBonus.Value = false;
    }
}
