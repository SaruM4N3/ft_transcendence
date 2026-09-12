using TMPro;
using Unity.Netcode;
using UnityEngine;

// World Space Canvas above this player, same pattern as InteractPromptUI's per-NPC prompt. Only shown
// during an actual multiplayer session - solo play has nobody else to label for.
public class PlayerNameTag : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameLabel;

    public void SetName(string value)
    {
        if (nameLabel != null)
            nameLabel.text = value;

        bool isMultiplayerSession = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        gameObject.SetActive(isMultiplayerSession);
    }
}
