---
graph-size: 200
---
#module #game

Système "press E" générique pour interagir avec les PNJ/objets du monde.

## Scripts

- [InteractableZone.cs](../../../UnityProject/Assets/Scripts/Interaction/InteractableZone.cs) — marque un PNJ/objet comme interactible
- [InteractionManager.cs](../../../UnityProject/Assets/Scripts/Interaction/InteractionManager.cs) — suit la zone la plus proche à portée du joueur
- [InteractPromptUI.cs](../../../UnityProject/Assets/Scripts/Interaction/InteractPromptUI.cs) — prompt "press E" en World Space, fixe par PNJ

## Connexions

- Les 4 PNJ du Lobby (Customize / Upgrades / Mode Select / Multiplayer) ouvrent chacun un [[UI]]`/MenuPanel` via ce système
