using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Friends.Models;
using UnityEngine;
using UnityEngine.UI;

// Friends menu: shows own shareable name, add-by-name input, and friends / incoming / sent-request rows.
public class FriendsPanelUI : MonoBehaviour
{
    [SerializeField] private FriendRowUI rowTemplate;
    [SerializeField] private TMP_Text ownNameText;
    [SerializeField] private TMP_InputField addInput;
    [SerializeField] private Button addButton;
    [SerializeField] private Button copyNameButton;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button closeButton;

    private static readonly Color ErrorColor = new Color(0.75f, 0.15f, 0.15f);
    private static readonly Color InfoColor = new Color(0.2f, 0.12f, 0.08f);
    private readonly List<GameObject> rows = new List<GameObject>();

    private void Awake()
    {
        rowTemplate.gameObject.SetActive(false);
        addButton.onClick.AddListener(AddFriend);
        if (copyNameButton != null)
            copyNameButton.onClick.AddListener(CopyOwnName);
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
    }

    private void OnEnable()
    {
        FriendsManager.OnChanged += Rebuild;
        Connect();
    }

    // Also runs when the parent multiplayer panel closes, so this overlay never reappears stale on reopen.
    private void OnDisable()
    {
        FriendsManager.OnChanged -= Rebuild;
        gameObject.SetActive(false);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    private async void Connect()
    {
        SetStatus("Connecting...", isError: false);
        try
        {
            await FriendsManager.InitializeAsync();
            SetStatus(string.Empty, isError: false);
        }
        catch (Exception e)
        {
            Debug.LogError($"FriendsPanelUI: failed to connect - {e.Message}");
            SetStatus("Couldn't reach the friends service.", isError: true);
        }
        Rebuild();
    }

    private void Rebuild()
    {
        foreach (GameObject row in rows)
            Destroy(row);
        rows.Clear();

        ownNameText.text = FriendsManager.IsReady ? FriendsManager.OwnName : string.Empty;
        addButton.interactable = FriendsManager.IsReady;

        foreach (Relationship request in FriendsManager.Incoming)
            AddRow(request, "Request", "Accept", () => Run(FriendsManager.AcceptAsync(request.Member.Id)),
                "Decline", () => Run(FriendsManager.DeclineAsync(request.Member.Id)));

        foreach (Relationship friend in FriendsManager.Friends)
            AddRow(friend, PresenceTag(friend), "Invite", () => Invite(friend.Member.Id),
                "Remove", () => Run(FriendsManager.RemoveAsync(friend.Member.Id)));

        foreach (Relationship request in FriendsManager.Outgoing)
            AddRow(request, "Request", "Cancel", () => Run(FriendsManager.CancelRequestAsync(request.Member.Id)));
    }

    // Online for any reachable availability, Offline otherwise.
    private static string PresenceTag(Relationship friend)
    {
        Availability availability = friend.Member.Presence?.Availability ?? Availability.Unknown;
        bool online = availability == Availability.Online || availability == Availability.Busy || availability == Availability.Away;
        return online ? "Online" : "Offline";
    }

    private void AddRow(Relationship relationship, string tag, string primaryText, Action onPrimary, string secondaryText = null, Action onSecondary = null)
    {
        FriendRowUI row = Instantiate(rowTemplate, rowTemplate.transform.parent);
        row.gameObject.SetActive(true);
        row.Setup(relationship.Member.Profile.Name, tag, primaryText, onPrimary, secondaryText, onSecondary);
        rows.Add(row.gameObject);
    }

    private async void AddFriend()
    {
        string friendName = addInput.text.Trim();
        if (string.IsNullOrEmpty(friendName))
        {
            SetStatus("Enter a player name (Name#1234).", isError: true);
            return;
        }

        addButton.interactable = false;
        try
        {
            await FriendsManager.AddByNameAsync(friendName);
            addInput.text = string.Empty;
            SetStatus("Request sent.", isError: false);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"FriendsPanelUI: add friend failed - {e.Message}");
            SetStatus("Couldn't add that player - check the name.", isError: true);
        }
        addButton.interactable = FriendsManager.IsReady;
    }

    // Starts hosting first when the local player isn't in a session yet, then sends the invite.
    private async void Invite(string memberId)
    {
        if (string.IsNullOrEmpty(FriendsManager.CurrentSessionCode))
        {
            NetworkBootstrap bootstrap = FindAnyObjectByType<NetworkBootstrap>(FindObjectsInactive.Include);
            if (bootstrap == null)
            {
                SetStatus("Can't host from here.", isError: true);
                return;
            }

            SetStatus("Creating your game...", isError: false);
            if (!await bootstrap.HostGameAsync())
            {
                SetStatus("Couldn't start hosting, try again.", isError: true);
                return;
            }
        }

        Run(FriendsManager.InviteAsync(memberId), "Invite sent.");
    }

    // Runs a row action and surfaces failures in the status line instead of losing them in an async void.
    private async void Run(Task action, string successMessage = "")
    {
        try
        {
            await action;
            SetStatus(successMessage, isError: false);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"FriendsPanelUI: action failed - {e.Message}");
            SetStatus("That didn't work, try again.", isError: true);
        }
    }

    private void CopyOwnName()
    {
        GUIUtility.systemCopyBuffer = FriendsManager.OwnName;
        SetStatus("Name copied.", isError: false);
    }

    private void SetStatus(string message, bool isError)
    {
        statusText.text = message;
        statusText.color = isError ? ErrorColor : InfoColor;
    }
}
