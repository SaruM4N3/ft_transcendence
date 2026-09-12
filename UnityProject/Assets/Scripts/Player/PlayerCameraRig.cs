using UnityEngine;

// Centralizes camera-child lookups by name, used in multiple places, so a prefab rename only touches one spot.
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

    // Reparents the player's camera(s) so they survive the player being deactivated.
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
