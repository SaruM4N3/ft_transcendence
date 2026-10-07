---
graph-size: 200
---
#module #game

Génération procédurale de la map. `ProceduralMapGenerator` est une classe `partial` éclatée par préoccupation. **Note** : la génération de plateformes/murs/escaliers a été supprimée (2026-09-08) — le terrain actuel est uniquement land/water/décor plat.

## Scripts

- [Procedural/ProceduralMapGenerator.cs](../../../UnityProject/Assets/Scripts/Terrain/Procedural/ProceduralMapGenerator.cs) — boucle de streaming de chunks, `System.Random` seedé par chunk pour une régénération déterministe
- [Procedural/ProceduralMapGenerator.Noise.cs](../../../UnityProject/Assets/Scripts/Terrain/Procedural/ProceduralMapGenerator.Noise.cs) — sampling de bruit Perlin pour biomes/eau
- [Procedural/ProceduralMapGenerator.Decor.cs](../../../UnityProject/Assets/Scripts/Terrain/Procedural/ProceduralMapGenerator.Decor.cs) — placement du décor
- [Procedural/Editor/ProceduralMapGeneratorEditor.cs](../../../UnityProject/Assets/Scripts/Terrain/Procedural/Editor/ProceduralMapGeneratorEditor.cs) — inspecteur custom
- [DecorFlipbook.cs](../../../UnityProject/Assets/Scripts/Terrain/DecorFlipbook.cs) — sprites de décor animés en flipbook
- [SiblingWallRuleTile.cs](../../../UnityProject/Assets/Scripts/Terrain/SiblingWallRuleTile.cs) — `TileBase` pour le matching de tiles de mur (hérité de l'époque où les murs existaient — à vérifier si encore utilisé)
- [Editor/SortingLayer_AutoEditor.cs](../../../UnityProject/Assets/Scripts/Terrain/Editor/SortingLayer_AutoEditor.cs) — inspecteur custom pour `SortingLayer_Auto` (voir [[Core]])

## Connexions

- Le décor utilise [[Core]]`/SortingLayer_Auto` pour son tri de rendu
- Testable isolément via la scène `Test/MapGenTest.unity` (voir [[Game Overview]])
