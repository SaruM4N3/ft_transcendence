using Unity.Netcode;

/// <summary>True for the offline solo player (never spawned) or the local owner of a spawned one -
/// false only for a spawned instance owned by someone else.</summary>
public static class NetworkBehaviourExtensions
{
    public static bool IsLocallyControlled(this NetworkBehaviour behaviour)
        => !behaviour.NetworkObject.IsSpawned || behaviour.IsOwner;
}
