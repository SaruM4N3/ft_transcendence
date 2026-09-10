using TMPro;
using Unity.Netcode;
using UnityEngine;

/// <summary>A World Space Canvas sitting at a fixed local position above this specific player, so it
/// moves with them for free via the Transform hierarchy - same pattern as InteractPromptUI's
/// per-NPC prompt. Driven by PlayerCustomization's replicated name NetworkVariable. Only ever shown
/// while an actual multiplayer session is running - solo play has nobody else to label for.</summary>
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
