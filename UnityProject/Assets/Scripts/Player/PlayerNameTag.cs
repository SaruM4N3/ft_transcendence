using TMPro;
using Unity.Netcode;
using UnityEngine;

// World-space name tag above a player, shown only in multiplayer.
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
