---
graph-size: 200
---
#module #game

Couche partagée entre [[Player]] et [[Enemies]] : interfaces de dégâts/santé, FX d'attaque, retours visuels.

## Scripts

- [IDamageable.cs](../../../UnityProject/Assets/Scripts/Combat/IDamageable.cs) — contrat "peut recevoir des dégâts", implémenté par `Player.Stats`, `EnemyStats`, `DummyStats`
- [IHealthStats.cs](../../../UnityProject/Assets/Scripts/Combat/IHealthStats.cs) — contrat lecture de vie/mana pour l'UI
- [IAttackFX.cs](../../../UnityProject/Assets/Scripts/Combat/IAttackFX.cs) — contrat FX par capacité, piloté par [[ClassKit]]
- [SlashAttackFX.cs](../../../UnityProject/Assets/Scripts/Combat/SlashAttackFX.cs) — FX de slash (attaque légère)
- [LayeredAttackFX.cs](../../../UnityProject/Assets/Scripts/Combat/LayeredAttackFX.cs) — FX double-couche pour les Specials circulaires
- [ShieldHoldFX.cs](../../../UnityProject/Assets/Scripts/Combat/ShieldHoldFX.cs) — FX du bouclier ultimate (Warrior)
- [ArrowProjectile.cs](../../../UnityProject/Assets/Scripts/Combat/ArrowProjectile.cs) — projectile (Archer / ennemis à distance)
- [WorldHealthBar.cs](../../../UnityProject/Assets/Scripts/Combat/WorldHealthBar.cs) — barre de vie au-dessus de la tête
- [DamageNumberSpawner.cs](../../../UnityProject/Assets/Scripts/Combat/DamageNumberSpawner.cs) / [DamageNumberPopup.cs](../../../UnityProject/Assets/Scripts/Combat/DamageNumberPopup.cs) — nombres de dégâts flottants
- [SpriteHitFlash.cs](../../../UnityProject/Assets/Scripts/Combat/SpriteHitFlash.cs) — flash blanc au hit
- [DummyStats.cs](../../../UnityProject/Assets/Scripts/Combat/DummyStats.cs) — cible d'entraînement (implémente `IDamageable`, variante `Dummy_Ally` pour tester heal/buff)

## Connexions

- Déclenché par [[Player]] (`Player.Actions`) et [[Enemies]] (`EnemyActions`)
- Cible toujours une `IDamageable` : joueur, ennemi, ou dummy
- FX réseau : la position d'un effet est **toujours recalculée localement**, jamais envoyée brute via RPC (piège connu, voir [[Networking]])
