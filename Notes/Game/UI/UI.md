---
graph-size: 200
---
#module #game

Tous les scripts d'interface. Pattern dominant : les composants gameplay ([[Player]], [[Enemies]]) exposent l'état via des **static C# events**, et l'UI s'abonne en `OnEnable`/se désabonne en `OnDisable`.

## HUD en jeu

- [StatBarUI.cs](../../../UnityProject/Assets/Scripts/UI/StatBarUI.cs) — barre vie/mana générique, pilotée par `Player.Stats`
- [XPBarUI.cs](../../../UnityProject/Assets/Scripts/UI/XPBarUI.cs) — barre d'XP
- [CooldownIcon.cs](../../../UnityProject/Assets/Scripts/UI/CooldownIcon.cs) — cooldown radial par `AbilityType` (voir [[Player]])
- [PerfStatsHUD.cs](../../../UnityProject/Assets/Scripts/UI/PerfStatsHUD.cs) — overlay FPS/ping
- [PlayerNameUI.cs](../../../UnityProject/Assets/Scripts/UI/PlayerNameUI.cs) — label de nom HUD
- [CoopHUD.cs](../../../UnityProject/Assets/Scripts/UI/CoopHUD.cs) — timer + vague en haut de l'écran (état répliqué de `WaveSpawner`, voir [[Enemies]])
- [AllyIndicatorManager.cs](../../../UnityProject/Assets/Scripts/UI/AllyIndicatorManager.cs) — flèches en bord d'écran vers les alliés hors-champ
- [ScreenEdgeFlash.cs](../../../UnityProject/Assets/Scripts/UI/ScreenEdgeFlash.cs) — flash d'écran (dégâts reçus, etc.)

## Menus / panneaux

- [MenuPanel.cs](../../../UnityProject/Assets/Scripts/UI/MenuPanel.cs) — composant générique d'ouverture/fermeture animée (scale+alpha via `CanvasGroup`, unscaled time), base des 4 menus PNJ du Lobby (voir [[Interaction]])
- [CharacterCustomizationMenu.cs](../../../UnityProject/Assets/Scripts/UI/CharacterCustomizationMenu.cs) — choix classe/couleur/nom, lié à `Player.Customization`
- [SettingsPanelUI.cs](../../../UnityProject/Assets/Scripts/UI/SettingsPanelUI.cs) / [SettingsTabGroup.cs](../../../UnityProject/Assets/Scripts/UI/SettingsTabGroup.cs) — onglets Général/Keybinds
- [KeybindsPanelUI.cs](../../../UnityProject/Assets/Scripts/UI/KeybindsPanelUI.cs) — rebind touches/manette
- [GameOverUI.cs](../../../UnityProject/Assets/Scripts/UI/GameOverUI.cs) — écran de défaite (Restart/Lobby)
- [LevelUpUI.cs](../../../UnityProject/Assets/Scripts/UI/LevelUpUI.cs) — écran de choix de montée de niveau

## Multijoueur

- [LobbyRosterUI.cs](../../../UnityProject/Assets/Scripts/UI/LobbyRosterUI.cs) / [LobbyRosterEntryUI.cs](../../../UnityProject/Assets/Scripts/UI/LobbyRosterEntryUI.cs) — liste des joueurs dans le Lobby
- [ReadyCheckUI.cs](../../../UnityProject/Assets/Scripts/UI/ReadyCheckUI.cs) / [ReadyCheckEntryUI.cs](../../../UnityProject/Assets/Scripts/UI/ReadyCheckEntryUI.cs) — ready-check avant lancement de mode

## Divers

- [BackgroundColorUI.cs](../../../UnityProject/Assets/Scripts/UI/BackgroundColorUI.cs) — icône d'épée qui s'adapte à la couleur de personnage choisie
- [AvatarUI.cs](../../../UnityProject/Assets/Scripts/UI/AvatarUI.cs) — portrait avatar
- [CursorManager.cs](../../../UnityProject/Assets/Scripts/UI/CursorManager.cs) — curseur logiciel custom (pulse au clic, handoff vers le curseur OS hors fenêtre)
- [DebugLogger.cs](../../../UnityProject/Assets/Scripts/UI/DebugLogger.cs) — overlay de logs debug

## Connexions

- → [[Player]], [[Enemies]] : consomme leurs static events
- → [[Networking]] : roster/ready-check reflètent l'état de session
- → [[Interaction]] : les 4 menus PNJ du Lobby sont des `MenuPanel`
