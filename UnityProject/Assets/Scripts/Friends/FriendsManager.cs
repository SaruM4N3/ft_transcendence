using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Friends;
using Unity.Services.Friends.Models;
using Unity.Services.Friends.Notifications;
using Unity.Services.Multiplayer;

[Serializable]
public class FriendInvite
{
    public string SessionCode;
    public string SenderName;
}

[Serializable]
public class PresenceActivity
{
    public string Status = "Lobby";
}

// Thin static wrapper over Unity Friends: lazy init, cached own name, and a single OnChanged event for the UI.
public static class FriendsManager
{
    public static event Action OnChanged;
    public static event Action<FriendInvite> OnInviteReceived;

    private static readonly IReadOnlyList<Relationship> Empty = new List<Relationship>();
    private static Task initTask;

    public static bool IsReady { get; private set; }
    public static string OwnName { get; private set; } = string.Empty;

    public static IReadOnlyList<Relationship> Friends => IsReady ? FriendsService.Instance.Friends : Empty;
    public static IReadOnlyList<Relationship> Incoming => IsReady ? FriendsService.Instance.IncomingFriendRequests : Empty;
    public static IReadOnlyList<Relationship> Outgoing => IsReady ? FriendsService.Instance.OutgoingFriendRequests : Empty;

    // Safe to call repeatedly - concurrent callers share one init; a failed init can be retried.
    public static Task InitializeAsync()
    {
        if (IsReady)
            return Task.CompletedTask;

        if (initTask == null || initTask.IsFaulted || initTask.IsCanceled)
            initTask = InitializeInternalAsync();
        return initTask;
    }

    private static async Task InitializeInternalAsync()
    {
        await ServicesAuth.EnsureSignedInAsync();
        await FriendsService.Instance.InitializeAsync();

        OwnName = await AuthenticationService.Instance.GetPlayerNameAsync();

        FriendsService.Instance.RelationshipAdded += _ => OnChanged?.Invoke();
        FriendsService.Instance.RelationshipDeleted += _ => OnChanged?.Invoke();
        FriendsService.Instance.PresenceUpdated += _ => OnChanged?.Invoke();
        FriendsService.Instance.MessageReceived += HandleMessage;

        await FriendsService.Instance.SetPresenceAsync(Availability.Online, new PresenceActivity());

        IsReady = true;
        OnChanged?.Invoke();
    }

    // Ignores anything that isn't a well-formed session invite from a friend.
    private static void HandleMessage(IMessageReceivedEvent message)
    {
        FriendInvite invite;
        try
        {
            invite = message.GetAs<FriendInvite>();
        }
        catch (Exception)
        {
            return;
        }

        if (invite != null && !string.IsNullOrEmpty(invite.SessionCode))
            OnInviteReceived?.Invoke(invite);
    }

    // Code of the session the local player is currently in (hosting or joined), or null when solo.
    public static string CurrentSessionCode
    {
        get
        {
            foreach (var pair in MultiplayerService.Instance.Sessions)
                if (!string.IsNullOrEmpty(pair.Value.Code))
                    return pair.Value.Code;
            return null;
        }
    }

    // Name without the "#1234" suffix, for showing to players.
    public static string ShortName(string fullName)
    {
        if (string.IsNullOrEmpty(fullName))
            return string.Empty;

        int hash = fullName.IndexOf('#');
        return hash > 0 ? fullName.Substring(0, hash) : fullName;
    }

    // Sends the current session's join code to a friend; only reaches them if they're online.
    public static Task InviteAsync(string memberId)
    {
        string code = CurrentSessionCode;
        if (string.IsNullOrEmpty(code))
            throw new InvalidOperationException("No active session to invite to.");

        return FriendsService.Instance.MessageAsync(memberId, new FriendInvite { SessionCode = code, SenderName = OwnName });
    }

    // Sends a request, or accepts one if the other player already sent theirs.
    public static async Task AddByNameAsync(string playerName)
    {
        await FriendsService.Instance.AddFriendByNameAsync(playerName);
        OnChanged?.Invoke();
    }

    public static async Task AcceptAsync(string memberId)
    {
        await FriendsService.Instance.AddFriendAsync(memberId);
        OnChanged?.Invoke();
    }

    public static async Task DeclineAsync(string memberId)
    {
        await FriendsService.Instance.DeleteIncomingFriendRequestAsync(memberId);
        OnChanged?.Invoke();
    }

    public static async Task CancelRequestAsync(string memberId)
    {
        await FriendsService.Instance.DeleteOutgoingFriendRequestAsync(memberId);
        OnChanged?.Invoke();
    }

    public static async Task RemoveAsync(string memberId)
    {
        await FriendsService.Instance.DeleteFriendAsync(memberId);
        OnChanged?.Invoke();
    }
}
