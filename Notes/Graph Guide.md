#guide

Lien retour : [[Home]].

Comment lire la vue **Local Graph** d'Obsidian (panneau latéral) sur ce vault, et ce que veut dire chaque couleur.

## Code couleur

Chaque nœud est coloré selon le **dossier** de la note (via les Color groups natifs d'Obsidian, synchronisés vers le Local Graph par le plugin "Sync Graph Settings"). Un lien prend la couleur de son nœud source.

Les 15 teintes sont réparties sur une roue chromatique (écart égal entre chaque couleur, pour qu'aucune ne se confonde avec sa voisine). Les cercles ci-dessous sont une approximation visuelle (il n'existe que 9 émojis cercle colorés pour 15 teintes) — la couleur exacte fait foi.

| Note | Dossier | Couleur |
|---|---|---|
| 🔴 Player | `Game/Player/` | `#e03e3e` |
| 🟠 Game Overview | `Game/` | `#e07f3e` |
| 🟡 UI | `Game/UI/` | `#e0bf3e` |
| 🟢 Enemies | `Game/Enemies/` | `#bfe03e` |
| 🟢 Combat | `Game/Combat/` | `#7fe03e` |
| 🟢 Networking | `Game/Networking/` | `#3ee03e` |
| 🟢 Core | `Game/Core/` | `#3ee07f` |
| 🔵 Web Overview | `Web/` | `#3ee0bf` |
| 🔵 Terrain | `Game/Terrain/` | `#3ebfe0` |
| 🔵 Frontend | `Web/Frontend/` | `#3e7fe0` |
| 🔵 Interaction | `Game/Interaction/` | `#3e3ee0` |
| 🟣 Server | `Web/Server/` | `#7f3ee0` |
| 🟣 Home | `Notes/` (racine) | `#bf3ee0` |
| 🟣 Friends | `Game/Friends/` | `#e03ebf` |
| 🔴 Infra | `Web/Infra/` | `#e03e7f` |

## Navigation

- **Cliquer un nœud** ouvre la note correspondante, ce qui recentre et zoome automatiquement le Local Graph dessus (comportement natif du plugin Extended Graph, pas besoin de recentrer à la main).
- **Profondeur** (combien de sauts de liens sont affichés autour de la note active) : réglée à **2**.
- La note actuellement ouverte est surlignée (accent du thème), indépendamment des couleurs de dossier ci-dessus.

## Attachments (images, etc.)

Un fichier ajouté depuis une note atterrit **dans le même dossier que cette note** (réglage "Default location for attachments" = dossier courant), et hérite donc automatiquement de la couleur de ce dossier.

⚠️ Exception : les liens vers les scripts `.cs` (`UnityProject/` est exclu du vault) apparaissent comme des nœuds gris non colorables — ce ne sont pas de vrais attachments mais des **liens non résolus** vers des fichiers hors du vault indexé, Obsidian n'a aucun moyen de leur assigner une couleur.

## Réglages du layout

- Force de répulsion entre nœuds : **15**
- Limite de nœuds gérés par Extended Graph : **300** (au-delà, le plugin affiche un avertissement et se désactive)
