---
graph-size: 300
---
#Main #game

Vue d'ensemble du projet Unity (`UnityProject/`, Unity 6000.5.6f1). Jeu top-down "Vampire Survivors"-like multijoueur visant 3 modes : co-op 4 joueurs, 2v2, et 3v1 asymétrique (horde leader). Seul le **co-op** est implémenté dans le code actuel ; 2v2 et horde-leader restent à faire.

Seul le **build WebGL exporté** de ce projet est destiné à atterrir dans `web/public/game/` — voir [[Web Overview]]. Le code source Unity n'a aucun lien avec `web/` autrement.

## Scènes (`UnityProject/Assets/Scenes/`)

- `Lobby.unity` — scène d'entrée (build index 0), pas de scène "main menu" séparée
- `Coop.unity` — mode co-op
- `Test/GameplayTest.unity`, `Test/MapGenTest.unity` — scènes de test, hors flow livré

## Modules de scripts (`UnityProject/Assets/Scripts/`)

- [[Player]] — mouvement, stats, classes, customisation, mount/pig-form
- [[Combat]] — interfaces de dégâts, FX d'attaque, barres de vie, dégâts flottants
- [[Enemies]] — IA horde, flow field, vagues, XP/level-up, game over
- [[Networking]] — Netcode + Relay, session, ready-check, gates offline
- [[Terrain]] — génération procédurale de map (chunks, biomes, décor)
- [[UI]] — HUD, menus, lobby roster, settings/keybinds
- [[Core]] — pause, settings persistés, loading screen, sorting layer auto
- [[Interaction]] — système "press E" pour les PNJ du Lobby
- [[Friends]] — liste d'amis + invitations

## Pattern transversal

Beaucoup de modules communiquent via des **static C# events** plutôt que des références directes (ex. `Player.Stats` → `UI/StatBarUI`). Les composants `NetworkBehaviour` gèrent la synchronisation réseau par-dessus cette couche gameplay locale — voir [[Networking]] pour le détail owner/server.
