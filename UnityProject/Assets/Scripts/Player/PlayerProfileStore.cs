using System;
using System.IO;
using UnityEngine;

// Local JSON persistence (Application.persistentDataPath) for class/color/name, independent of any
// network session or account. Placeholder ahead of the web app's own user management.
public static class PlayerProfileStore
{
    [Serializable]
    private class ProfileData
    {
        public int classIndex;
        public int colorIndex;
        public string playerName = "";
    }

    private static readonly string FilePath = Path.Combine(Application.persistentDataPath, "player_profile.json");

    public static bool TryLoad(out int classIndex, out int colorIndex, out string playerName)
    {
        classIndex = 0;
        colorIndex = 0;
        playerName = "";

        if (!File.Exists(FilePath))
            return false;

        try
        {
            ProfileData data = JsonUtility.FromJson<ProfileData>(File.ReadAllText(FilePath));
            if (data == null)
                return false;

            classIndex = data.classIndex;
            colorIndex = data.colorIndex;
            playerName = data.playerName ?? "";
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"PlayerProfileStore: failed to load {FilePath}, ignoring saved profile. {e.Message}");
            return false;
        }
    }

    public static void Save(int classIndex, int colorIndex, string playerName)
    {
        ProfileData data = new ProfileData
        {
            classIndex = classIndex,
            colorIndex = colorIndex,
            playerName = playerName ?? ""
        };

        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(data));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"PlayerProfileStore: failed to save {FilePath}. {e.Message}");
        }
    }
}
