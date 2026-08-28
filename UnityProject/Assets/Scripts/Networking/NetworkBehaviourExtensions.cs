using Unity.Netcode;

/// <summary>True for the offline solo player (never spawned) or the local owner of a spawned one -
/// false only for a spawned instance owned by someone else.</summary>
public static class NetworkBehaviourExtensions
{
    public static bool IsLocallyControlled(this NetworkBehaviour behaviour)
        => !behaviour.NetworkObject.IsSpawned || behaviour.IsOwner;

    /// <summary>Server-authority equivalent of IsLocallyControlled - true offline (solo play, never
    /// spawned) or on the server; false on a spawned instance for anyone else (a joined client).</summary>
    public static bool HasServerAuthority(this NetworkBehaviour behaviour)
        => !behaviour.NetworkObject.IsSpawned || behaviour.IsServer;
}
