---
graph-size: 200
---
#module #game

Systèmes transversaux, pas spécifiques à un module gameplay.

## Scripts

- [PauseManager.cs](../../../UnityProject/Assets/Scripts/Core/PauseManager.cs) — flag statique `IsPaused` lu par [[Player]] et d'autres ; gère les panneaux pause/settings et le bouton de sortie contextuel ("Return to Lobby" / "Leave Session")
- [GameSettings.cs](../../../UnityProject/Assets/Scripts/Core/GameSettings.cs) — paramètres persistés (voir [[UI]]`/SettingsPanelUI`)
- [KeybindOverrides.cs](../../../UnityProject/Assets/Scripts/Core/KeybindOverrides.cs) — rebind touches/manette persisté
- [LoadingScreenManager.cs](../../../UnityProject/Assets/Scripts/Core/LoadingScreenManager.cs) — écran de chargement lors des transitions de scène
- [SortingLayer_Auto.cs](../../../UnityProject/Assets/Scripts/Core/SortingLayer_Auto.cs) — assignation automatique de sorting layer/order
- [DeathProtectionZone.cs](../../../UnityProject/Assets/Scripts/Core/DeathProtectionZone.cs) — zone d'invulnérabilité temporaire après respawn *(déplacé récemment depuis `Scripts/Player/`, changement non commité au moment de la rédaction de cette note)*

## Connexions

- `PauseManager.IsPaused` est vérifié par [[Player]]`/Player.Movement`
- `SortingLayer_Auto` est utilisé par le décor de [[Terrain]]
- `DeathProtectionZone` s'articule avec le flow de mort/mount de [[Player]]
