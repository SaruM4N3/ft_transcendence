using Unity.Netcode;

// True for the offline solo player or the owner of a spawned one.
public static class NetworkBehaviourExtensions
{
    public static bool IsLocallyControlled(this NetworkBehaviour behaviour)
        => behaviour.NetworkObject != null && (!behaviour.NetworkObject.IsSpawned || behaviour.IsOwner);

    // True offline or on the server; false for a joined client's spawned copy.
    public static bool HasServerAuthority(this NetworkBehaviour behaviour)
        => behaviour.NetworkObject != null && (!behaviour.NetworkObject.IsSpawned || behaviour.IsServer);
}
