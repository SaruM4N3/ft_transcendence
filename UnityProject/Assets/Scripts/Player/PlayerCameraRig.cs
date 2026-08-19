using UnityEngine;

// The player prefab's camera children are looked up by name in more than one place (disabling them on
// a remote puppet, detaching them while the offline player is deactivated for Host/Join) - centralized
// here so a prefab rename only needs to update one place.
public static class PlayerCameraRig
{
    private const string MainCameraName = "Main Camera";
    private const string CinemachineCameraName = "CinemachineCamera";

    public static void SetActive(Transform player, bool active)
    {
        Transform mainCamera = player.Find(MainCameraName);
        if (mainCamera != null)
            mainCamera.gameObject.SetActive(active);

        Transform cinemachineCamera = player.Find(CinemachineCameraName);
        if (cinemachineCamera != null)
            cinemachineCamera.gameObject.SetActive(active);
    }

    // Reparents the player's camera(s) under a new holder object, e.g. so they survive the player being
    // deactivated. Returns the holder, or null if the player had no camera children.
    public static GameObject Detach(Transform player)
    {
        Transform mainCamera = player.Find(MainCameraName);
        Transform cinemachineCamera = player.Find(CinemachineCameraName);
        if (mainCamera == null && cinemachineCamera == null)
            return null;

        GameObject holder = new GameObject("DetachedPlayerCameraRig");
        if (mainCamera != null)
            mainCamera.SetParent(holder.transform, worldPositionStays: true);
        if (cinemachineCamera != null)
            cinemachineCamera.SetParent(holder.transform, worldPositionStays: true);
        return holder;
    }
}
