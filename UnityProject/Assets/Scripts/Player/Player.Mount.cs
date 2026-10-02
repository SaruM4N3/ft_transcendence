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

    // Real Netcode parenting instead of copying position each frame; only the server reparents, and LateUpdateMount applies the offset once that replicates back (writing it immediately would snap under the stale old parent).
    private void StartMounting(Player pig)
    {
        if (!this.IsLocallyControlled() || isMounted || pig == null || !pig.IsDead)
            return;

        isMounted = true;
        mountedPig = pig;
        mountElapsed = 0f;

        if (rb != null)
            rb.bodyType = RigidbodyType2D.Kinematic;

        if (NetworkObject.IsSpawned)
        {
            RequestMountParentServerRpc(pig.NetworkObject);
        }
        else
        {
            transform.SetParent(pig.transform, worldPositionStays: false);
            transform.localPosition = mountedOffset;
            transform.localRotation = Quaternion.identity;
        }
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

    // worldPositionStays must be true: false would reinterpret the rider's old world position as a local offset from the pig, teleporting them for one frame before LateUpdateMount corrects it.
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestMountParentServerRpc(NetworkObjectReference pigRef)
    {
        if (pigRef.TryGet(out NetworkObject pigNetworkObject))
            NetworkObject.TrySetParent(pigNetworkObject, worldPositionStays: true);
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

    // Unparents before reviving: both go out as separate RPCs, and the revive effect could otherwise land on observers before the unparent does, dragging the rider along behind the now-moving revived player.
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
            Player pigToRevive = mountedPig;
            StopMounting();
            pigToRevive.RequestRevive();
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

    // Hides the "press E to revive" prompt once someone is riding; runs off the replicated transform.parent, not a local-only flag, same as UpdateMountCollisionIgnore.
    private void UpdateMountInteractVisibility()
    {
        if (mountInteractRoot == null || !IsDead)
            return;

        bool shouldShow = !HasRider();
        if (mountInteractRoot.activeSelf != shouldShow)
            mountInteractRoot.SetActive(shouldShow);
    }

    private bool HasRider()
    {
        foreach (Player candidate in AllActiveInstances)
        {
            if (candidate != this && candidate.transform.parent == transform)
                return true;
        }
        return false;
    }

    // Local rendering only, runs for every observer; auto Y-sort is unreliable this close, so pin just in front of the pig instead.
    private void LateUpdateMountSorting()
    {
        Player parentPlayer = transform.parent != null ? transform.parent.GetComponent<Player>() : null;

        if (sortingLayerAuto != null)
            sortingLayerAuto.enabled = parentPlayer == null;

        if (parentPlayer == null || spriteRenderer == null || parentPlayer.spriteRenderer == null)
            return;

        int pigSortingOrder = parentPlayer.sortingLayerAuto != null
            ? parentPlayer.sortingLayerAuto.CurrentSortingOrder
            : parentPlayer.spriteRenderer.sortingOrder;
        spriteRenderer.sortingOrder = pigSortingOrder + mountedSortingOrderBump;
    }
}
