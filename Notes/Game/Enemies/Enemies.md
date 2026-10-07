---
graph-size: 200
---
#module #game

IA de la horde. Même pattern de découpage que [[Player]] : un hub `Enemy.cs` + `EnemyAI`/`EnemyActions` séparés, un seul prefab de base configuré par un asset de données (`EnemyKit`, l'équivalent de `ClassKit` côté joueur).

## Scripts

- [Enemy.cs](../../../UnityProject/Assets/Scripts/Enemies/Enemy.cs) — hub
- [EnemyAI.cs](../../../UnityProject/Assets/Scripts/Enemies/EnemyAI.cs) — décision/déplacement (flow field)
- [EnemyActions.cs](../../../UnityProject/Assets/Scripts/Enemies/EnemyActions.cs) — attaques, inflige des dégâts via `IDamageable`
- [EnemyStats.cs](../../../UnityProject/Assets/Scripts/Enemies/EnemyStats.cs) — vie, implémente `IDamageable`/`IHealthStats` (voir [[Combat]])
- [EnemyKit.cs](../../../UnityProject/Assets/Scripts/Enemies/EnemyKit.cs) — asset de données par type d'ennemi (tier, portée, stats)
- [Editor/EnemyKitEditor.cs](../../../UnityProject/Assets/Scripts/Enemies/Editor/EnemyKitEditor.cs) — inspecteur custom
- [EnemyFlowFieldManager.cs](../../../UnityProject/Assets/Scripts/Enemies/EnemyFlowFieldManager.cs) / [EnemyFlowField.cs](../../../UnityProject/Assets/Scripts/Enemies/EnemyFlowField.cs) — un flow field partagé par joueur pour le pathing de masse
- [ObstacleQuery.cs](../../../UnityProject/Assets/Scripts/Enemies/ObstacleQuery.cs) — requêtes d'obstacles pour le flow field
- [AttackTelegraph.cs](../../../UnityProject/Assets/Scripts/Enemies/AttackTelegraph.cs) — tell visuel avant une attaque
- [RangedAttackVisual.cs](../../../UnityProject/Assets/Scripts/Enemies/RangedAttackVisual.cs) — visuel d'attaque à distance
- [WaveSpawner.cs](../../../UnityProject/Assets/Scripts/Enemies/WaveSpawner.cs) — spawn autour des joueurs (pas au centre de la map), scale avec le nombre de joueurs
- [XPManager.cs](../../../UnityProject/Assets/Scripts/Enemies/XPManager.cs) / [LevelUpManager.cs](../../../UnityProject/Assets/Scripts/Enemies/LevelUpManager.cs) — gain d'XP, écran de choix de level-up (solo illimité / multi 20s+ready-list)
- [GameOverCheck.cs](../../../UnityProject/Assets/Scripts/Enemies/GameOverCheck.cs) — réplique un wipe total, l'host choisit Restart/Lobby

## Connexions

- → [[Combat]] : inflige/reçoit des dégâts via `IDamageable`
- → [[UI]] : `LevelUpManager`/`GameOverCheck` pilotent `LevelUpUI`/`GameOverUI`, `CoopHUD` affiche timer+vague depuis l'état répliqué de `WaveSpawner`
- → [[Networking]] : état répliqué (vagues, wipe) synchronisé par Netcode
