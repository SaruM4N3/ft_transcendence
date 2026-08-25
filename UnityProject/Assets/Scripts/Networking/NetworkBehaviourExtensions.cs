using Unity.Netcode;

/// <summary>A player prefab instance can exist two ways: placed directly in a scene for offline/solo
/// play (never spawned through Netcode), or spawned by NetworkManager once a session starts. Gameplay
/// scripts should treat both as "mine to control" - only a spawned instance actually owned by someone
/// else should be excluded.</summary>
public static class NetworkBehaviourExtensions
{
    public static bool IsLocallyControlled(this NetworkBehaviour behaviour)
        => !behaviour.NetworkObject.IsSpawned || behaviour.IsOwner;
}
