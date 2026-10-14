using Unity.Netcode;

public partial class Player
{
    private readonly NetworkVariable<bool> hasChosenLevelUpBonus = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<int> chosenLevelUpBonusIndex = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public bool HasChosenLevelUpBonus => hasChosenLevelUpBonus.Value;
    public int ChosenLevelUpBonusIndex => chosenLevelUpBonusIndex.Value;

    // Called by this player's own level-up UI.
    public void ChooseLevelUpBonus(int bonusIndex)
    {
        if (!this.IsLocallyControlled())
            return;

        ChooseLevelUpBonusLocal(bonusIndex);
    }

    // Called by LevelUpManager (server) on a player that didn't pick before the timer ran out.
    public void ForceLevelUpBonus(int bonusIndex)
    {
        if (!NetworkObject.IsSpawned)
        {
            ChooseLevelUpBonusLocal(bonusIndex);
            return;
        }
        ForceLevelUpBonusClientRpc(bonusIndex, new ClientRpcParams
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
    private void ForceLevelUpBonusClientRpc(int bonusIndex, ClientRpcParams rpcParams = default)
    {
        ChooseLevelUpBonusLocal(bonusIndex);
    }

    [ClientRpc]
    private void ResetLevelUpChoiceClientRpc(ClientRpcParams rpcParams = default)
    {
        ResetLevelUpChoiceLocal();
    }

    private void ChooseLevelUpBonusLocal(int bonusIndex)
    {
        hasChosenLevelUpBonus.Value = true;
        chosenLevelUpBonusIndex.Value = bonusIndex;
    }

    private void ResetLevelUpChoiceLocal()
    {
        hasChosenLevelUpBonus.Value = false;
        chosenLevelUpBonusIndex.Value = -1;
    }
}
