---
graph-size: 200
---
#module #web

Front actuel : pas de framework, scaffold minimal.

## Fichiers

- [public/index.html](../../../web/public/index.html), [public/script.js](../../../web/public/script.js), [public/style.css](../../../web/public/style.css) — front actuel (pas de framework)
- `public/game/` — **build WebGL exporté** du projet Unity (généré, pas du code source — voir [[Game Overview]]), avec `Build/`, `TemplateData/`, `StreamingAssets/`

## Connexions

- Le seul lien avec [[Game Overview]] est le dépôt du build WebGL dans `public/game/` — sinon les deux codebases sont indépendantes
- Servi par [[Server]] (`server.js`)
- `server.js` doit fixer `Content-Length` pour que le cache IndexedDB du build WebGL détecte correctement une nouvelle version
