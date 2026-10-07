---
graph-size: 200
---
#module #game

Tout ce qui concerne le joueur contrôlable. `Player.cs` est le hub : une classe `partial` éclatée en plusieurs fichiers par responsabilité (fusion effectuée lors du refactor "Player component merge").

## Scripts

- [Player.cs](../../../UnityProject/Assets/Scripts/Player/Player.cs) — hub, état partagé entre les fichiers partiels ci-dessous
- [Player.Movement.cs](../../../UnityProject/Assets/Scripts/Player/Player.Movement.cs) — input (New Input System), déplacement, flip sprite, orientation souris
- [Player.Actions.cs](../../../UnityProject/Assets/Scripts/Player/Player.Actions.cs) — Attack/Special/Ultimate pilotés par [[ClassKit]]
- [Player.Stats.cs](../../../UnityProject/Assets/Scripts/Player/Player.Stats.cs) — vie/mana, implémente `IDamageable`/`IHealthStats` (voir [[Combat]])
- [Player.Customization.cs](../../../UnityProject/Assets/Scripts/Player/Player.Customization.cs) — classe/couleur/nom
- [Player.Feedback.cs](../../../UnityProject/Assets/Scripts/Player/Player.Feedback.cs) — retours visuels (hit flash, etc.)
- [Player.LevelUp.cs](../../../UnityProject/Assets/Scripts/Player/Player.LevelUp.cs) — réception des choix de level-up
- [Player.Mount.cs](../../../UnityProject/Assets/Scripts/Player/Player.Mount.cs) — monter le cochon d'un allié pour le ressusciter
- [Player.PigForm.cs](../../../UnityProject/Assets/Scripts/Player/Player.PigForm.cs) — forme "cochon" quand mort (se déplace, ne combat pas)
- [Player.Shield.cs](../../../UnityProject/Assets/Scripts/Player/Player.Shield.cs) — ultimate bouclier du Warrior
- [ClassKit.cs](../../../UnityProject/Assets/Scripts/Player/ClassKit.cs) — asset de données par classe (stats + FX par capacité)
- [Editor/ClassKitEditor.cs](../../../UnityProject/Assets/Scripts/Player/Editor/ClassKitEditor.cs) — inspecteur custom pour `ClassKit`
- [AbilityType.cs](../../../UnityProject/Assets/Scripts/Player/AbilityType.cs) — enum partagé avec `UI/CooldownIcon`
- [LocalPlayer.cs](../../../UnityProject/Assets/Scripts/Player/LocalPlayer.cs) — référence/marqueur du joueur local
- [MouseDirectionIndicator.cs](../../../UnityProject/Assets/Scripts/Player/MouseDirectionIndicator.cs) — rune au sol pointant la souris, teintée à la couleur de customisation
- [PlayerCameraRig.cs](../../../UnityProject/Assets/Scripts/Player/PlayerCameraRig.cs) — caméra suiveuse / caméra de preview customisation
- [PlayerNameTag.cs](../../../UnityProject/Assets/Scripts/Player/PlayerNameTag.cs) — nametag monde, visible seulement en multi
- [PlayerProfileStore.cs](../../../UnityProject/Assets/Scripts/Player/PlayerProfileStore.cs) — sauvegarde locale JSON (classe/couleur/nom)
- [PlayerSpawner.cs](../../../UnityProject/Assets/Scripts/Player/PlayerSpawner.cs) / [PlayerSpawnPoint.cs](../../../UnityProject/Assets/Scripts/Player/PlayerSpawnPoint.cs) — placement au spawn réseau

## Connexions

- → [[UI]] : `Player.Stats`/level-up/ability-used exposés via **static C# events**, consommés par `StatBarUI`, `XPBarUI`, `CooldownIcon`
- → [[Combat]] : `Player.Actions` déclenche les FX d'attaque (`IAttackFX`), `Player.Stats` implémente `IDamageable`
- → [[Networking]] : spawn/ownership via `PlayerSpawner`, logique owner-only gated par `IsLocallyControlled` (offline-first)
- → [[Core]] : `Player.Movement` vérifie `PauseManager.IsPaused`
- → [[Enemies]] : cible/est ciblé par `EnemyActions` via `IDamageable`
