using Unity.Netcode;
using UnityEngine;

public partial class Player
{
    [Header("Mount / Revive")]
    [SerializeField] private float mountDurationSeconds = 20f;
    [SerializeField] private GameObject mountInteractRoot;
    [SerializeField] private Vector3 mountedOffset = new Vector3(0f, 0.25f, 0f);
    [SerializeField] private int mountedSortingOrderBump = 5;

    private bool isMounted;
    private Player mountedPig;
    private float mountElapsed;
    private Player collisionIgnoredWith;

    public bool IsMounted => isMounted;
    public float MountProgress01 => isMounted ? Mathf.Clamp01(mountElapsed / mountDurationSeconds) : 0f;

    // Wired to the MountInteract zone's onInteract (a child of the downed player); starts mounting whichever local player interacted.
    public void RequestMountByLocalPlayer()
    {
        if (!IsDead)
            return;

        GameObject localPlayerObj = LocalPlayer.Get();
        Player localPlayer = localPlayerObj != null ? localPlayerObj.GetComponent<Player>() : null;
        if (localPlayer == null || localPlayer == this || localPlayer.IsDead)
            return;

        localPlayer.StartMounting(this);
    }

    private void StartMounting(Player pig)
    {
        if (!this.IsLocallyControlled() || isMounted || pig == null || !pig.IsDead)
            return;

        isMounted = true;
        mountedPig = pig;
        mountElapsed = 0f;

        if (rb != null)
            rb.bodyType = RigidbodyType2D.Kinematic;

        // Real Netcode parenting (zero relative lag) instead of copying position each frame; only the server may reparent.
        if (NetworkObject.IsSpawned)
            RequestMountParentServerRpc(pig.NetworkObject);
        else
            transform.SetParent(pig.transform, worldPositionStays: false);

        transform.localPosition = mountedOffset;
        transform.localRotation = Quaternion.identity;
    }

    private void StopMounting()
    {
        if (!isMounted)
            return;

        if (rb != null)
            rb.bodyType = RigidbodyType2D.Dynamic;

        if (NetworkObject.IsSpawned)
            RequestUnmountParentServerRpc();
        else
            transform.SetParent(null, worldPositionStays: true);

        isMounted = false;
        mountedPig = null;
        mountElapsed = 0f;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestMountParentServerRpc(NetworkObjectReference pigRef)
    {
        if (pigRef.TryGet(out NetworkObject pigNetworkObject))
            NetworkObject.TrySetParent(pigNetworkObject, worldPositionStays: false);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestUnmountParentServerRpc()
    {
        NetworkObject.TrySetParent((Transform)null, worldPositionStays: true);
    }

    // Runs on every client (each simulates physics locally); reads the replicated transform.parent, not the local isMounted flag.
    private void UpdateMountCollisionIgnore()
    {
        Player currentParent = transform.parent != null ? transform.parent.GetComponent<Player>() : null;
        if (currentParent == collisionIgnoredWith)
            return;

        if (collisionIgnoredWith != null && bodyCollider != null && collisionIgnoredWith.bodyCollider != null)
            Physics2D.IgnoreCollision(bodyCollider, collisionIgnoredWith.bodyCollider, false);

        collisionIgnoredWith = currentParent;

        if (collisionIgnoredWith != null && bodyCollider != null && collisionIgnoredWith.bodyCollider != null)
            Physics2D.IgnoreCollision(bodyCollider, collisionIgnoredWith.bodyCollider, true);
    }

    private void UpdateMount()
    {
        if (!isMounted || !this.IsLocallyControlled())
            return;

        if (IsDead || mountedPig == null || !mountedPig.IsDead)
        {
            StopMounting();
            return;
        }

        mountElapsed += Time.deltaTime;
        if (mountElapsed >= mountDurationSeconds)
        {
            mountedPig.RequestRevive();
            StopMounting();
        }
    }

    // Re-offsets once the server-driven parenting request has actually landed on this client; no-op once already in place.
    private void LateUpdateMount()
    {
        if (!isMounted || !this.IsLocallyControlled() || mountedPig == null)
            return;

        if (transform.parent == mountedPig.transform)
        {
            transform.localPosition = mountedOffset;
            transform.localRotation = Quaternion.identity;
        }
    }

    // Local rendering only, runs for every observer; auto Y-sort is unreliable this close, so pin just in front of the pig instead.
    private void LateUpdateMountSorting()
    {
        Player parentPlayer = transform.parent != null ? transform.parent.GetComponent<Player>() : null;

        if (sortingLayerAuto != null)
            sortingLayerAuto.enabled = parentPlayer == null;

        if (parentPlayer != null && spriteRenderer != null && parentPlayer.spriteRenderer != null)
            spriteRenderer.sortingOrder = parentPlayer.spriteRenderer.sortingOrder + mountedSortingOrderBump;
    }
}
