---
graph-size: 200
---
#module #game

Couche multijoueur : Netcode for GameObjects + Unity Relay/Services. Transversale à presque tous les autres modules.

## Scripts

- [NetworkBootstrap.cs](../../../UnityProject/Assets/Scripts/Networking/NetworkBootstrap.cs) — host/join via l'API Session Unity + Relay, lancé depuis le Lobby
- [ServicesAuth.cs](../../../UnityProject/Assets/Scripts/Networking/ServicesAuth.cs) — authentification Unity Services
- [GameModeLoader.cs](../../../UnityProject/Assets/Scripts/Networking/GameModeLoader.cs) — chargement de la scène de mode (Coop, …)
- [ModeReadyCheck.cs](../../../UnityProject/Assets/Scripts/Networking/ModeReadyCheck.cs) — ready-check / sélection d'équipe avant lancement
- [OfflinePlayerGate.cs](../../../UnityProject/Assets/Scripts/Networking/OfflinePlayerGate.cs) — garantit que le solo fonctionne sans hoster (pattern `IsLocallyControlled`, jamais `IsOwner` brut)
- [PhantomPlayerCleanup.cs](../../../UnityProject/Assets/Scripts/Networking/PhantomPlayerCleanup.cs) — supprime les spawns fantômes de `NetworkObject` placés en scène
- [SessionDisconnectHandler.cs](../../../UnityProject/Assets/Scripts/Networking/SessionDisconnectHandler.cs) — évite que le client freeze quand l'host quitte
- [NetworkBehaviourExtensions.cs](../../../UnityProject/Assets/Scripts/Networking/NetworkBehaviourExtensions.cs) — helpers partagés
- [TestClientProbe.cs](../../../UnityProject/Assets/Scripts/Networking/TestClientProbe.cs) — harnais de test 2 instances (Editor host + build standalone)

## Connexions

- Sous-couche de [[Player]] (spawn/ownership), [[Enemies]] (état répliqué des vagues), [[Friends]] (invite ↔ session hosting)
- Pilote [[UI]] : `LobbyRosterUI`, `ReadyCheckUI`, `InviteToastUI`
- Piège récurrent : `NetworkVariable.Value` ne doit jamais être écrit en premier dans `Awake()` (crash WebGL/IL2CPP) ; ajouter/retirer une `NetworkVariable` nécessite un rebuild lockstep (sinon `OverflowException` sur un client avec un vieux build)
