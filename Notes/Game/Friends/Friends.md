---
graph-size: 200
---
#module #game

Liste d'amis et invitations à une session.

## Scripts

- [FriendsManager.cs](../../../UnityProject/Assets/Scripts/Friends/FriendsManager.cs) — logique d'invitation (anti-spam ; inviter en hostant ferme d'abord ta propre session en cours)
- [FriendsPanelUI.cs](../../../UnityProject/Assets/Scripts/Friends/FriendsPanelUI.cs) / [FriendRowUI.cs](../../../UnityProject/Assets/Scripts/Friends/FriendRowUI.cs) — liste UI des amis
- [InviteToastUI.cs](../../../UnityProject/Assets/Scripts/Friends/InviteToastUI.cs) — toast d'invitation entrante

## Connexions

- → [[Networking]] : une invitation pointe vers une session hostée via `NetworkBootstrap`
