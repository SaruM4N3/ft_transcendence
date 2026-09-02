using System;
using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

// Shows a friend's game invite with Join/Dismiss; sits on an always-active object so it keeps listening while the toast is hidden.
public class InviteToastUI : MonoBehaviour
{
    [SerializeField] private GameObject content;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button dismissButton;
    [SerializeField] private NetworkBootstrap bootstrap;
    [SerializeField] private float autoHideSeconds = 30f;

    private string pendingCode;
    private Coroutine hideRoutine;

    private void Awake()
    {
        content.SetActive(false);
        joinButton.onClick.AddListener(Join);
        dismissButton.onClick.AddListener(Hide);
    }

    private void OnEnable()
    {
        FriendsManager.OnInviteReceived += Show;
    }

    private void OnDisable()
    {
        FriendsManager.OnInviteReceived -= Show;
    }

    // Connects to the friends service up front so invites arrive even if the friends panel was never opened.
    private async void Start()
    {
        try
        {
            await FriendsManager.InitializeAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"InviteToastUI: friends service unavailable, invites disabled - {e.Message}");
        }
    }

    private void Show(FriendInvite invite)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            return;

        pendingCode = invite.SessionCode;
        messageText.text = $"{FriendsManager.ShortName(invite.SenderName)} invited you to play";
        content.SetActive(true);

        if (hideRoutine != null)
            StopCoroutine(hideRoutine);
        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSecondsRealtime(autoHideSeconds);
        Hide();
    }

    private void Hide()
    {
        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }
        content.SetActive(false);
    }

    private async void Join()
    {
        string code = pendingCode;
        Hide();
        await bootstrap.JoinWithCodeAsync(code);
    }
}
